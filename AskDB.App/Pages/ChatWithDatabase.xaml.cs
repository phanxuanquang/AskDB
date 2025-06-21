using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.Commons.Extensions;
using AskDB.SemanticKernel.Factories;
using AskDB.SemanticKernel.InvocationFilters;
using AskDB.SemanticKernel.Plugins;
using AskDB.SemanticKernel.Services;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseInteractor.Factories;
using DatabaseInteractor.Helpers;
using DatabaseInteractor.Services;
using GeminiDotNET;
using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;
using GeminiDotNET.ApiModels.Enums;
using GeminiDotNET.FunctionCallings.Attributes;
using GeminiDotNET.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Pdf;
using Windows.Networking.NetworkOperators;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using WinRT.Interop;
using static Dapper.SqlMapper;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace AskDB.App.Pages
{
    public sealed partial class ChatWithDatabase : Page, IFunctionInvocationFilter, IAutoFunctionInvocationFilter
    {
        private readonly ObservableCollection<ChatContent> Messages = [];
        private readonly ObservableCollection<AgentSuggestion> AgentSuggestions = [];

        private Generator _generator;
        private DatabaseInteractionService _databaseInteractor;
        private string _globalInstruction;
        private string _actionPlanInstruction;

        private KernelFactory _kernelFactory;
        private AgentChatCompletionService _chatCompletionService;

        public ChatWithDatabase()
        {
            this.InitializeComponent();
        }

        #region Function Declarations
        [FunctionDeclaration("change_the_conversation_language", "Change the conversation language for the agent. This will update the global instruction to reflect the new language setting.")]
        private void ChangeTheConversationLanguage(string language)
        {
            _globalInstruction = _globalInstruction.Replace("{Language}", language);
        }

        [FunctionDeclaration("request_for_action_plan", @"Request an action plan based on the current situation.
This function triggers a **higher-level reasoning process** to generate a structured, step-by-step **action plan** that responds to ambiguous, uncertain, or complex situations.

---

### **Purpose**

Use this function when you:

* Encounter **ambiguity, uncertainty**, or a **lack of sufficient context** to safely proceed.
* Face **persistent tool errors** or **unexpected database responses** that can’t be resolved with retries or parameter tweaks.
* Need to **formulate a multi-step strategy** to fulfill a complex or vague user request.
* Detect that the current path is **risky, error-prone, or poorly defined**, and continuing may lead to unsafe operations or user confusion.

---

### **When to Call This Function**

#### Error Escalation / Recovery

* A function call (e.g., `get_table_structure`, `execute_query`) fails **repeatedly**, and the error:

  * Is unclear or undocumented
  * Indicates **permission issues** or **schema anomalies**
  * Suggests deeper structural problems (e.g., missing objects, circular dependencies)

> *“Query failed due to missing columns, but column names appear correct. Retrying won’t help — request an action plan.”*

#### High-Level Ambiguous Request

* The user asks for something **broad, unclear, or strategic**, such as:

  * “Help me explore this database.”
  * “Can you optimize this whole workflow?”
  * “What's the best way to analyze our customer churn?”

> *“Too vague for direct SQL — need a structured breakdown with decision points.”*

####  Complex Task Decomposition

* A task clearly requires **multiple steps** involving:

  * Several tool calls
  * Conditional branches
  * Safe sequencing (e.g., inspect → validate → execute → verify)
* But the **flow is not yet established**, or too risky to guess.

> *“The user wants to delete inactive accounts. You need to check constraints, foreign keys, and ask for confirmation — request an action plan.”*

#### Stuck or Unsafe

* The current approach feels **unsafe**, or you’re unsure whether continuing will:

  * Violate constraints
  * Cause irreversible actions
  * Misunderstand the user’s real intent

> *“The table has no primary key. A DELETE is requested. Need help deciding how to proceed safely.”*

#### Collaboration Trigger

* You believe this situation needs:

  * Another agent to step in
  * Supervisor logic
  * A structured review before taking action

> *“The user might be asking something outside current capabilities or boundaries — escalate.”*

---

### **What This Function Is NOT For**

* Not for simple query planning
* Not for debugging syntax errors (try re-running or adjusting the query first)
* Not for interacting directly with the user (this is an **internal reasoning function**)
* Not for retrying the same call with minor parameter tweaks

---

### *Best Practices**

* Treat this as a signal for **multi-step reasoning** or **error resolution**.
* Use when you're **not confident** in how to proceed and want a **structured resolution path**.
* Expect the output to be an **ordered list of actions** (e.g., “First call function X, then verify Y, finally run Z”).")]
        private async Task<string> RequestForActionPlanAsync()
        {
            try
            {
                var actionPlanRequest = new ApiRequestBuilder()
                    .WithSystemInstruction(_actionPlanInstruction.Replace("{Database_Type}", _databaseInteractor.DatabaseType.ToString()))
                    .DisableAllSafetySettings()
                    .WithTools(new ToolBuilder().AddFunctionDeclarations(FunctionDeclarationHelper.FunctionDeclarations))
                    .SetFunctionCallingMode(FunctionCallingMode.NONE)
                    .WithPrompt("I have some blocking points here and need you to make an action plan to overcome. Now think deeply step-by-step about the current situation, then provide a clear and detailed action plan.")
                    .Build();

                var actionPlanResponse = await _generator.GenerateContentAsync(actionPlanRequest, ModelVersion.Gemini_20_Flash);

                return actionPlanResponse.Content;
            }
            catch (Exception ex)
            {
                return $"**Error:** {ex.Message}";
            }
        }

        [FunctionDeclaration("request_for_internet_search", @"Perform an in-depth **Google Search** to retrieve external information from the internet.
Use this function **ONLY** when essential details are unavailable through internal knowledge or schema-aware tools, and you need **outside context** to fulfill or enhance a **database-related request**.

---

### **Purpose**

Use this function to:

* Understand technical concepts, error codes, or behaviors **outside current database metadata or tool-based introspection**.
* Fetch **external documentation**, **real-world examples**, **best practices**, or **community discussions** relevant to SQL, schema design, database configuration, or performance optimization.
* Incorporate **non-schema-specific industry knowledge** to better fulfill the user’s request.

---

### **When to Use**

#### **Unknown Concepts / Terminology**

* The user references:

  * A feature or standard outside your training data (e.g., “ISO 20022 for financial databases”).
  * An acronym, pattern, or methodology that you can't infer (e.g., “explain PAXOS in distributed SQL”).

> *“I don’t understand this protocol or term — look it up externally.”*

---

#### **Error Code Lookup**

* You encounter an **error code or message** (e.g., `ORA-00933`, `SQLSTATE[HY000]`) that is:

  * Unfamiliar or vendor-specific
  * Not solvable through retry or schema inspection

> *“The query fails with a SQL error I've never seen — search for solutions.”*

---

#### **Feature-Specific Guidance**

* The user asks about:

  * **New or niche features** in SQL engines (e.g., `MERGE` syntax in MySQL 8.0)
  * Differences between database engine versions (e.g., “Has Azure SQL enabled `STRING_AGG` yet?”)
  * Usage of **vendor-specific extensions** (e.g., `Oracle Autonomous Database` or `Snowflake Streams`)

> *“Schema tools won’t show if the feature is available — check official documentation.”*

---

#### **Search for Best Practices / Examples**

* You need to find:

  * Real-world SQL examples (e.g., recursive CTEs, window functions)
  * Community-vetted **performance tuning tips**
  * **Best practices for indexing**, partitioning, or materialized views

> *“This join is slow — look up tuning strategies for large hash joinsr.”*

---

#### **Cross-Domain Context**

* The user query relates to **external, domain-specific events** or **non-database content**:

  * “Find users impacted by the recent regulation posted on `example.com`”
  * “Correlate sales with the new Apple product release schedule”

> *“I need public information to correlate internal data meaningfully.”*

---

### **Repeatable Patterns Where This Helps**

| Use Case                      | Why External Search Helps                    |
| ----------------------------- | -------------------------------------------- |
| Error analysis                | Schema inspection won’t explain vendor error |
| Advanced SQL usage            | Complex queries need proven examples         |
| Feature detection by version  | Schema tools don’t show feature availability |
| Performance tuning            | Real-world advice is often external          |
| Vendor-specific syntax quirks | Requires specific documentation or examples  |
| Domain-specific policy lookup | Schema doesn’t reflect external regulations  |
| Strategy formulation          | Need broader perspective to plan or validate |

---

### **When NOT to Use**

* DO NOT search for table names, column types, or schema details — use schema tools like `get_table_structure`, `search_tables_by_name`, or `get_database_schema`.
* DO NOT search for answers that can be retrieved using `execute_query` or simple metadata queries.
* DO NOT use for general browsing or user entertainment purposes.
* DO NOT use vague or broad queries — queries must be **targeted and contextualized**.

---

### **Important Notes**

* **Be specific**: Always include the full context in the query, e.g., `""SQL Server 2022 error 3621 MERGE statement fails when nulls in source""`.
* **Internal first**: Exhaust all internal schema-aware reasoning before falling back to internet search.
* **Fallback mode**: Only use when essential to solving the user’s request safely and accurately.

---

### Summary

Use this function to retrieve **critical, missing context** from the internet when:

* Schema tools and internal knowledge fall short
* A specific external error or domain concept is blocking progress
* You need best practices, version differences, or vendor-specific behavior clarification

> Use it responsibly. Prioritize internal analysis. Be precise. Always focus on **database-relevant** augmentation.")]
        private async Task<string> RequestForInternetSearchAsync(string requirement)
        {
            try
            {
                var searchRequest = new ApiRequestBuilder()
                .WithSystemInstruction(_globalInstruction)
                .WithTools(new ToolBuilder().EnableGoogleSearch())
                .WithDefaultGenerationConfig(0.5F)
                .DisableAllSafetySettings()
                .WithPrompt($"Now perform an **in-depth internet research**, then provide a detailed reported in markdown format. The search result MUST satify the below requirement:\n\n{requirement}")
                .Build();

                var generator = new Generator(Cache.ApiKey);

                var searchResponse = await generator.GenerateContentAsync(searchRequest, ModelVersion.Gemini_20_Flash);
                return searchResponse.Content;
            }
            catch (Exception ex)
            {
                return $"**Error:** {ex.Message}";
            }
        }
        #endregion

        #region Event Handlers
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not DatabaseConnectionInfo request)
            {
                return;
            }

            SetLoading(true, "Working");


            try
            {
                _databaseInteractor = request.DatabaseType.CreateDatabaseInteractionService(request.ConnectionString);

                var globalTask = OnlineContentHelper.GetSytemInstructionContentAsync("Global", _databaseInteractor.DatabaseType, "English");
                var actionPlanTask = OnlineContentHelper.GetSytemInstructionContentAsync("Action Plan", _databaseInteractor.DatabaseType, "English");
                var initQueryTemplatesTask = _databaseInteractor.InitQueryTemplatesAsync();

                await Task.WhenAll(globalTask, actionPlanTask, initQueryTemplatesTask);

                _globalInstruction = globalTask.Result;
                _actionPlanInstruction = actionPlanTask.Result;

                ChangeTheConversationLanguage("English");

                _kernelFactory = new KernelFactory()
                    .UseGoogleGeminiProvider(Cache.ApiKey, "gemini-2.0-flash")
                    .WithPlugin(new DatabaseInteractionPlugin(_databaseInteractor))
                    .WithAutoFunctionInvocationFilter(this)
                    .WithFunctionInvocationFilter(this);

                _chatCompletionService = new AgentChatCompletionService(_kernelFactory)
                    .WithSystemInstruction(_globalInstruction);

                await LoadTableNamesAsync();

                await HandleUserInputAsync("search for tables that contain the word 'customer' in their name, then show me the table structure of the first 3 tables found.");
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                await DialogHelper.ShowErrorAsync($"{ex.Message}.\nThe error details have been copied to your clipboard.");
                Frame.GoBack();
            }
            finally
            {
                SetLoading(false);
            }
        }
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable? dataTable = ((sender as Button)?.DataContext as ChatContent)?.Data;

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    await DialogHelper.ShowErrorAsync("No data available to export.");
                    return;
                }

                var savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = DateTime.Now.ToString("yy.MM.dd-HH.mm.ss").Replace(".", string.Empty)
                };
                savePicker.FileTypeChoices.Add("CSV", [".csv"]);

                nint windowHandle = WindowNative.GetWindowHandle(App.Window);
                InitializeWithWindow.Initialize(savePicker, windowHandle);

                var file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    await dataTable.ToCsvAsync(file.Path);
                    await ShowInforBarAsync("Exported", true);
                }
            }
            catch (Exception ex)
            {
                await ShowInforBarAsync(ex.Message, false);
            }
        }
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var userInput = QueryBox.Text.Trim();
            QueryBox.Text = string.Empty;

            if (string.IsNullOrEmpty(userInput))
            {
                return;
            }

            await HandleUserInputAsync(userInput);
        }
        private void QueryBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var dataGrid = sender as DataGrid;

            if (dataGrid == null || dataGrid?.DataContext is not ChatContent context || (context.Data == null || context.Data.Rows.Count == 0))
            {
                return;
            }

            dataGrid.Columns.Clear();

            for (int i = 0; i < context.Data.Columns.Count; i++)
            {
                dataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = context.Data.Columns[i].ColumnName,
                    Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                });
            }
        }
        private async void SuggestionItemView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not AgentSuggestion suggestion)
            {
                return;
            }

            var userInput = suggestion.UserResponseSuggestion;

            if (string.IsNullOrEmpty(userInput))
            {
                return;
            }

            await HandleUserInputAsync(userInput);
        }
        private async void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            var uri = new Uri(e.Link);
            await Launcher.LaunchUriAsync(uri);
        }
        #endregion

        #region Message and Progress Content Bindings
        private void SetLoading(bool isLoading, string? message = null)
        {
            LoadingIndicator.SetLoading(message, isLoading);
            LoadingOverlay.Visibility = VisibilityHelper.SetVisible(isLoading);
            MessageSpace.Opacity = isLoading ? 0.5 : 1;
        }
        private void SetUserMessage(string message)
        {
            var ChatContent = new ChatContent
            {
                Message = message,
                IsFromUser = true,
                IsFromAgent = false,
            };

            Messages.Add(ChatContent);
        }

        private void SetAgentMessage(string? message, DataTable? dataTable = null)
        {
            var isDataTableEmpty = dataTable == null || dataTable.Rows.Count == 0;

            var ChatContent = new ChatContent
            {
                Message = string.IsNullOrEmpty(message) ? string.Empty : message,
                IsFromUser = false,
                IsFromAgent = true,
                QueryResults = isDataTableEmpty ? [] : new ObservableCollection<object>(dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray)),
                Data = isDataTableEmpty ? null : dataTable
            };

            Messages.Add(ChatContent);
        }
        #endregion

        private async Task HandleUserInputAsync(string userMessage)
        {
            try
            {
                SetLoading(true, "Working");
                SetUserMessage(userMessage);

                AgentSuggestionsItemView.Visibility = VisibilityHelper.SetVisible(false);
                AgentSuggestions.Clear();

                while (true)
                {
                    var result = await _chatCompletionService.SendMessageAsync(userMessage, _chatCompletionService.ServiceProvider.CreatePromptExecutionSettings());

                    if (!string.IsNullOrEmpty(result.Content))
                    {
                        SetAgentMessage(result.Content);
                        break;
                    }

                    SetAgentMessage("**Error:** No response from the agent. Please try again later.");
                }

            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                SetAgentMessage($"**Error:** {ex.Message}. {ex.InnerException?.Message}.\n\nThe reason detail has been copied to your clipboard.");
            }
            finally
            {
                SetLoading(false);
            }
        }
        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var message = ((sender as Button)?.DataContext as ChatContent)?.Message;

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(message);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();

            await ShowInforBarAsync("Copied", true);
        }

        private async Task ShowInforBarAsync(string message, bool isSuccess)
        {
            MessageInfoBar.Title = message;
            MessageInfoBar.IsOpen = true;
            MessageInfoBar.Severity = isSuccess ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            await Task.Delay(750);
            MessageInfoBar.IsOpen = false;
        }

        private async Task<List<string>> LoadTableNamesAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Request to read your table names",
                Content = "AskDB would like permission to read the list of table names in your database. This helps AskDB to better understand your data structure and provide more accurate, helpful actions during your requests.\n\nYour data stays safe, only table names are read.",
                PrimaryButtonText = "Allow",
                SecondaryButtonText = "Skip",
                XamlRoot = App.Window.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var tableNames = await _databaseInteractor.SearchTablesByNameAsync(string.Empty, null);
                _databaseInteractor.CachedAllTableNames.UnionWith(tableNames);

                return tableNames;
            }

            return [];
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (Messages.Count == 0)
            {
                await DialogHelper.ShowErrorAsync("You have not sent any message to AskDB");
            }

            var result = await DialogHelper.ShowDialogWithOptions("Reset the conversation", "This action will clear our conversation. Are you sure to proceed?", "Yes");
            if (result == ContentDialogResult.Primary)
            {
                _generator = new Generator(Cache.ApiKey).EnableChatHistory(200);
                Messages.Clear();
                AgentSuggestions.Clear();

                try
                {
                    SetLoading(true);
                    var requestForAgentSuggestions = new ApiRequestBuilder()
                        .WithSystemInstruction(_globalInstruction)
                        .DisableAllSafetySettings()
                        .WithDefaultGenerationConfig()
                        .WithPrompt("In order to start the conversation, please introduce to me about yourself, such as who you are, what you can do, what you can help me, or anything else; and at least 3 best practices for me to help you to do the task effectively. Take me as your friend or your teammate, avoid to use formal-like tone while talking to me; just use a natural, friendly tone with daily-life word when talking to me, like you are talking with your friends in the real life.")
                        .Build();

                    var response = await _generator.GenerateContentAsync(requestForAgentSuggestions, ModelVersion.Gemini_20_Flash_Lite);

                    if (!string.IsNullOrEmpty(response.Content))
                    {
                        SetAgentMessage(response.Content);
                    }
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            //if (context.Function.PluginName == nameof(DatabaseInteractionPlugin) && context.Function.Name == nameof(DatabaseInteractionPlugin.GetTableStructureDetailAsync))
            //{
            //    var schemaName = context.Arguments["schema"]?.ToString() ?? string.Empty;
            //    var tableName = context.Arguments["table"]?.ToString() ?? string.Empty;
            //    var tableStructure = await _databaseInteractor.GetTableStructureDetailAsync(schemaName, tableName);
            //    if (tableStructure.Rows.Count > 0)
            //    {
            //        SetAgentMessage(null, tableStructure);
            //    }
            //    else
            //    {
            //        SetAgentMessage($"No structure found for table '{tableName}'.");
            //    }
            //    return;
            //}

            await next(context);
        }

        public async Task CallFunctionAsync(FunctionInvocationContext context)
        {
            if (context.Function.Name == nameof(DatabaseInteractionPlugin.SearchTablesByNameAsync))
            {
                var keyword = context.Arguments["keyword"]?.ToString() ?? string.Empty;
                var maxResult = context.Arguments["maxResult"] as int? ?? 20000;
                var tableNames = await _databaseInteractor.SearchTablesByNameAsync(keyword, maxResult);
                if (tableNames.Count > 0)
                {
                    SetAgentMessage($"Found {tableNames.Count} tables matching `{keyword}`: {string.Join(", ", tableNames.Select(name => $"`{name}`"))}");
                }
                else
                {
                    SetAgentMessage($"No tables found matching `{keyword}`.");
                }
            }
            else if (context.Function.Name == nameof(DatabaseInteractionPlugin.GetUserPermissionsAsync))
            {
                var permissions = await _databaseInteractor.GetUserPermissionsAsync();
                if (permissions.Count > 0)
                {
                    SetAgentMessage($"You have the following permissions: {string.Join(", ", permissions)}");
                }
                else
                {
                    SetAgentMessage("You have no permissions in this database.");
                }
            }
            else if (context.Function.Name == nameof(DatabaseInteractionPlugin.ExecuteQueryAsync))
            {
                var sqlQuery = context.Arguments["sqlQuery"]?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(sqlQuery))
                {
                    SetAgentMessage("No SQL query provided.");
                    return;
                }
                try
                {
                    var resultDataTable = await _databaseInteractor.ExecuteQueryAsync(sqlQuery);
                    if (resultDataTable.Rows.Count > 0)
                    {
                        SetAgentMessage(null, resultDataTable);
                    }
                    else
                    {
                        SetAgentMessage("Query executed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    SetAgentMessage($"**Error:** {ex.Message}");
                }
            }
            else if (context.Function.Name == nameof(DatabaseInteractionPlugin.ExecuteNonQueryAsync))
            {
                var sqlCommand = context.Arguments["sqlCommand"]?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(sqlCommand))
                {
                    SetAgentMessage("No SQL command provided.");
                    return;
                }
                try
                {
                    await _databaseInteractor.ExecuteNonQueryAsync(sqlCommand);
                    SetAgentMessage("SQL command executed successfully.");
                }
                catch (Exception ex)
                {
                    SetAgentMessage($"**Error:** {ex.Message}");
                }
            }
            else if (context.Function.Name == nameof(DatabaseInteractionPlugin.GetTableStructureDetailAsync))
            {
                var schemaName = context.Arguments["schema"]?.ToString() ?? string.Empty;
                var tableName = context.Arguments["table"]?.ToString() ?? string.Empty;
                var tableStructure = await _databaseInteractor.GetTableStructureDetailAsync(schemaName, tableName);
                if (tableStructure.Rows.Count > 0)
                {
                    SetAgentMessage(null, tableStructure);
                }
                else
                {
                    SetAgentMessage($"No structure found for table '{tableName}'.");
                }
            }
            else
            {
                // Handle other function invocations if needed
            }
        }

        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            if (context.Function.PluginName == nameof(DatabaseInteractionPlugin) && context.Function.Name == nameof(DatabaseInteractionPlugin.GetTableStructureDetailAsync))
            {
                var schemaName = context.Arguments["schema"]?.ToString() ?? string.Empty;
                var tableName = context.Arguments["table"]?.ToString() ?? string.Empty;
                var tableStructure = await _databaseInteractor.GetTableStructureDetailAsync(schemaName, tableName);
                if (tableStructure.Rows.Count > 0)
                {
                    SetAgentMessage(null, tableStructure);
                }
                else
                {
                    SetAgentMessage($"No structure found for table '{tableName}'.");
                }
                return;
            }

            await next(context);
        }
    }
}
