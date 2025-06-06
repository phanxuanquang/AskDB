using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.Commons.Extensions;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseInteractor.Factories;
using DatabaseInteractor.Services;
using GeminiDotNET;
using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;
using GeminiDotNET.ApiModels.Enums;
using GeminiDotNET.ApiModels.Response.Success.FunctionCalling;
using GeminiDotNET.FunctionCallings.Attributes;
using GeminiDotNET.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;                                    
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace AskDB.App.Pages
{
    public sealed partial class ChatWithDatabase : Page
    {
        private readonly ObservableCollection<ChatMessage> Messages = [];
        private readonly ObservableCollection<ProgressContent> ProgressContents = [];
        private readonly ObservableCollection<AgentSuggestion> AgentSuggestions = [];

        private Generator _generator;
        private DatabaseInteractionService _databaseInteractor;
        private string _globalInstruction;

        public ChatWithDatabase()
        {
            this.InitializeComponent();
        }

        #region Function Declarations
        [FunctionDeclaration("change_the_conversation_language", "Change the conversation language for the agent. This will update the global instruction to reflect the new language setting.")]
        private void ChangeTheConversationLanguage(string language)
        {
            _globalInstruction = _globalInstruction
                .Replace("{Language}", language)
                .Replace("{DateTime_Now}", DateTime.Now.ToString("HH:mm:ss, mm.MM.yyyy"))
                .Replace("{Database_Type}", _databaseInteractor.DatabaseType.GetDescription());
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
            var instruction = await FileHelper.ReadFileAsync("Instructions/Action Plan.md");

            var actionPlanRequest = new ApiRequestBuilder()
                .WithSystemInstruction(instruction.Replace("{Database_Type}", _databaseInteractor.DatabaseType.ToString()))
                .DisableAllSafetySettings()
                .WithTools(new ToolBuilder().AddFunctionDeclarations(FunctionDeclarationHelper.FunctionDeclarations))
                .SetFunctionCallingMode(FunctionCallingMode.NONE)
                .WithPrompt("I have some blocking points here and need you to make an action plan to overcome. Now think deeply step-by-step about the current situation, then provide a clear and detailed action plan.")
                .Build();

            var actionPlanResponse = await _generator.GenerateContentAsync(actionPlanRequest, Cache.ReasoningModelAlias);

            return actionPlanResponse.Content;
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
        #endregion

        #region Event Handlers
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not DatabaseConnectionInfo request)
            {
                return;
            }

            SetLoading(true);

            _databaseInteractor = ServiceFactory.CreateInteractionService(request.DatabaseType, request.ConnectionString);
            _globalInstruction = await FileHelper.ReadFileAsync("Instructions/Global.md");

            ChangeTheConversationLanguage("English");

            var tableCount = await _databaseInteractor.GetTableCountAsync();
            if (tableCount > 200)
            {
                _globalInstruction = _globalInstruction.Replace("{Note_For_Table_Count}", "- **Note on Known Large Tables:** All tables are pre-identified as being extremely large. Any query against them MUST be treated as High-Risk and requires extra caution.");
            }
            else
            {
                _globalInstruction = _globalInstruction.Replace("{Note_For_Table_Count}", $"- The connected database has {tableCount} tables and you should be careful while querying them. Please consider consulting with the documentation before proceeding.");
            }

            _generator = new Generator(Cache.ApiKey).EnableChatHistory(150);

            InitFunctionCalling();

            SetLoading(false);
        }
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable? dataTable = ((sender as Button)?.DataContext as ProgressContent)?.Data;

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
                    await DialogHelper.ShowSuccessAsync("CSV file has been exported to your selected location.");
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync(ex.Message);
            }
        }
        private void ShowProgressToggle_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            MainView.IsPaneOpen = toggleButton?.IsChecked == true;
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

            if (dataGrid == null || dataGrid?.DataContext is not ProgressContent context || (context.Data == null || context.Data.Rows.Count == 0))
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
        private async void CopySqlButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var context = button?.DataContext as ProgressContent;

            if (string.IsNullOrEmpty(context?.SqlCommand))
            {
                return;
            }

            var package = new DataPackage();
            package.SetText(context.SqlCommand);
            Clipboard.SetContent(package);

            await DialogHelper.ShowSuccessAsync("SQL command has been copied to clipboard.");
        }
        #endregion

        #region Message and Progress Content Bindings
        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.SetLoading("Working", isLoading);
            LoadingOverlay.Visibility = VisibilityHelper.SetVisible(isLoading);
            MessageSpace.Opacity = isLoading ? 0.5 : 1;
        }
        private void SetUserMessage(string? message, bool isFromUser = false)
        {
            var chatMessage = new ChatMessage
            {
                Content = message,
                Alignment = isFromUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            Messages.Add(chatMessage);
        }
        private void SetProgressMessage(string? message)
        {
            var progressContent = new ProgressContent
            {
                Message = message,
                SqlCommand = null,
            };

            ProgressContents.Add(progressContent);
        }
        private void SetProgressDataTable(string? sqlCommand, DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return;
            }

            var progressContent = new ProgressContent
            {
                Message = null,
                SqlCommand = sqlCommand,
                QueryResults = new ObservableCollection<object>(dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray)),
                Data = dataTable
            };

            ProgressContents.Add(progressContent);
        }

        private void InitFunctionCalling()
        {
            FunctionDeclarationHelper.RegisterFunction(RequestForActionPlanAsync);
            FunctionDeclarationHelper.RegisterFunction(RequestForInternetSearchAsync, new Parameters
            {
                Properties = new
                {
                    requirement = new
                    {
                        type = "string",
                        description = @"A highly specific and detailed natural language requirement for the internet search in markdown format. 
It **MUST** include at least: 

1. A brief summarization of the current user request or problem. 
2. The specific information you are trying to find (documentation, examples, troubleshooting steps, etc.). 
3. How this information will help you address the user's database-related task.
4. Any specific sources or types of information you want to prioritize (e.g., official documentation, community forums, etc.).
5. Any other relevant context that can help refine the search results.
6. The search query should be clear and concise, avoiding vague terms or overly broad requests.   
7. Any specific formats you prefer for the results (e.g., list, table, etc.)."
                    }
                },
                Required = ["requirement"]
            });
            FunctionDeclarationHelper.RegisterFunction(ChangeTheConversationLanguage, new Parameters
            {
                Properties = new
                {
                    language = new
                    {
                        type = "string",
                        description = "The language to change the conversation to. This should be a valid language name (e.g., 'English', 'Spanish', 'French')."
                    }
                },
                Required = ["language"]
            });

            FunctionDeclarationHelper.RegisterFunction(_databaseInteractor.ExecuteQueryAsync, new Parameters
            {
                Properties = new
                {
                    sqlQuery = new
                    {
                        type = "string",
                        description = "The SQL query to execute. It should be a valid SQL command that returns data, such as a `SELECT` statement."
                    }
                },
                Required = ["sqlQuery"]
            });
            FunctionDeclarationHelper.RegisterFunction(_databaseInteractor.ExecuteNonQueryAsync, new Parameters
            {
                Properties = new
                {
                    sqlQuery = new
                    {
                        type = "string",
                        description = "The complete, syntactically correct SQL command or SQL scripts (e.g., INSERT, UPDATE, DELETE, CREATE, ALTER) to be executed. Ensure the query is specific to the user-confirmed action plan."
                    }
                },
                Required = ["sqlQuery"]
            });
            FunctionDeclarationHelper.RegisterFunction(_databaseInteractor.GetUserPermissionsAsync);
            FunctionDeclarationHelper.RegisterFunction(_databaseInteractor.SearchTablesByNameAsync, new Parameters
            {
                Properties = new
                {
                    keyword = new
                    {
                        type = "string",
                        description = "A keyword to filter the table names. For example, `user` might return `Users`, `UserAccounts`. If an empty string is provided, all accessible table names with their associated schema are returned. The search is typically case-insensitive and looks for the keyword within the table names."
                    }
                },
                Required = ["keyword"]
            });
            FunctionDeclarationHelper.RegisterFunction(_databaseInteractor.GetTableStructureDetailAsync, new Parameters
            {
                Properties = new
                {
                    schema = new
                    {
                        type = "string",
                        description = "The name of the schema containing the table. This is often case-sensitive depending on the database. If not explicitly provided by the user or clear from context, attempt to use the database's default schema (e.g., 'dbo' for SQL Server, 'public' for PostgreSQL). If still uncertain, clarify with the user or use 'SearchSchemasByNameAsync' to find possible schemas."
                    },
                    table = new
                    {
                        type = "string",
                        description = "The name of the table for which to retrieve detailed schema information. This is often case-sensitive depending on the database."
                    }
                },
                Required = ["schema", "table"]
            });
        }
        #endregion

        private async Task HandleUserInputAsync(string userInput)
        {
            try
            {
                SetLoading(true);
                SetUserMessage(userInput, true); 
                AgentSuggestions.Clear();

                var functionDeclarations = FunctionDeclarationHelper.FunctionDeclarations;

                var apiRequest = new ApiRequestBuilder()
                    .WithSystemInstruction(_globalInstruction)
                    .WithPrompt(userInput)
                    .DisableAllSafetySettings()
                    .WithTools(new ToolBuilder().AddFunctionDeclarations(functionDeclarations))
                    .SetFunctionCallingMode(FunctionCallingMode.AUTO)
                    .Build();

                var modelResponse = await _generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash);

                var functionCalls = (modelResponse.FunctionCalls == null || modelResponse.FunctionCalls.Count == 0) ? [] : modelResponse.FunctionCalls;

                if (functionCalls.Count == 0)
                {
                    SetUserMessage(modelResponse.Content, false);
                    return;
                }

                while (functionCalls.Count > 0)
                {
                    await Task.Delay(2000);

                    try
                    {
                        var functionResponses = new List<FunctionResponse>();

                        foreach (var function in functionCalls)
                        {
                            try
                            {
                                var functionResponse = await CallFunctionAsync(function);
                                if (functionResponse != null)
                                {
                                    functionResponses.Add(functionResponse);
                                }
                            }
                            catch (Exception ex)
                            {
                                var output = $"Error in function **{function.Name}**:\n\n```plaintext\n{ex.Message}. {ex.InnerException?.Message}\n```";

                                functionResponses.Add(new FunctionResponse
                                {
                                    Name = function.Name,
                                    Response = new Response
                                    {
                                        Output = $"{output}\n\nAction plan or clarification or internet search is required.",
                                    }
                                });

                                SetProgressMessage(output);
                            }
                        }

                        var apiRequestWithFunctions = new ApiRequestBuilder()
                            .WithSystemInstruction(_globalInstruction)
                            .DisableAllSafetySettings()
                            .WithTools(new ToolBuilder().AddFunctionDeclarations(functionDeclarations))
                            .SetFunctionCallingMode(FunctionCallingMode.AUTO)
                            .WithFunctionResponses(functionResponses)
                            .WithPrompt(@"Please review the function responses against the user's initial request to check if it has been fulfilled.

* **If not**, return the list of function declarations that still need to be called, as the **next steps to take action**.
* **If there is a blocking point** or clarification is needed, **do not return any function declarations**. Instead, provide the reponse as your monologue to **review your current context, your problems, and the user's expectations** in order to proceed effectively. This will be used as the context for the next action of agent.")
                            .Build();

                        var modelResponseForFunction = await _generator.GenerateContentAsync(apiRequestWithFunctions, ModelVersion.Gemini_20_Flash);
                        SetProgressMessage(modelResponseForFunction.Content);

                        functionCalls = (modelResponseForFunction.FunctionCalls == null || modelResponseForFunction.FunctionCalls.Count == 0) ? [] : modelResponseForFunction.FunctionCalls;
                    }
                    catch (Exception ex)
                    {
                        SetUserMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
                        functionCalls = [];
                    }
                }

                var requestForAgentResponse = new ApiRequestBuilder()
                           .WithSystemInstruction(_globalInstruction)
                           .DisableAllSafetySettings()
                           .WithDefaultGenerationConfig(0.5F)
                           .WithResponseSchema(new
                           {
                               type = "object",
                               properties = new
                               {
                                   Summarization = new
                                   {
                                       type = "string"
                                   },
                                   UserResponseSuggestions = new
                                   {
                                       type = "array",
                                       items = new
                                       {
                                           type = "string"
                                       }
                                   }
                               },
                               required = new[] { "Summarization", "UserResponseSuggestions" }
                           })
                           .WithPrompt("Please provide the summary of the current context, and the up to 5 short and concise suggestions for the next step in the viewpoint of the user for the user to use as the quick reply to the agent.")
                           .Build();

                var response = await _generator.GenerateContentAsync(requestForAgentResponse, ModelVersion.Gemini_20_Flash_Lite);
                var agentResponse = JsonHelper.AsObject<AgentResponse>(response.Content);

                if (agentResponse?.UserResponseSuggestions != null && agentResponse.UserResponseSuggestions.Count > 0)
                {
                    foreach (var suggestion in agentResponse.UserResponseSuggestions)
                    {
                        AgentSuggestions.Add(new AgentSuggestion
                        {
                            UserResponseSuggestion = suggestion
                        });
                    }
                }

                SetUserMessage(agentResponse.Summarization, false);
            }
            catch (Exception ex)
            {
                SetUserMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
                SetProgressMessage($"An error occurred while processing your request: {ex.Message}\n\n```json\n{_generator.HistoryContent.AsString()}\n```");
            }
            finally
            {
                SetLoading(false);
            }
        }
        private async Task<FunctionResponse> CallFunctionAsync(FunctionCall function)
        {
            switch (function.Name)
            {
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.ExecuteQueryAsync):
                    {
                        var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        SetProgressMessage($"Let me execute this query to check the data:\n\n```sql\n{sqlQuery}\n```");
                        var dataTable = await _databaseInteractor.ExecuteQueryAsync(sqlQuery);
                        SetProgressDataTable(sqlQuery, dataTable);
                        return FunctionCallingHelper.CreateResponse(name, dataTable.ToMarkdown());
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.ExecuteNonQueryAsync):
                    {
                        var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        SetProgressMessage($"Let me execute the command:\n\n```sql\n{sqlQuery}\n```");
                        await _databaseInteractor.ExecuteNonQueryAsync(sqlQuery);
                        SetProgressMessage("Command executed successfully.");
                        return FunctionCallingHelper.CreateResponse(name, $"Command executed successfully.\n\n```sql\n{sqlQuery}\n```");
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.GetTableStructureDetailAsync):
                    {
                        var schema = FunctionCallingHelper.GetParameterValue<string>(function, "schema");
                        var table = FunctionCallingHelper.GetParameterValue<string>(function, "table");
                        SetProgressMessage($"Let me check the schema information for the table `{schema}.{table}`");
                        var schemaInfo = await _databaseInteractor.GetTableStructureDetailAsync(schema, table);
                        SetProgressDataTable(null, schemaInfo);
                        return FunctionCallingHelper.CreateResponse(name, schemaInfo.ToMarkdown());
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.SearchTablesByNameAsync):
                    {
                        var keyword = FunctionCallingHelper.GetParameterValue<string>(function, "keyword");
                        if (string.IsNullOrEmpty(keyword))
                        {
                            SetProgressMessage("Searching for all accessible tables...");
                        }
                        else
                        {
                            SetProgressMessage($"Searching for tables using keyword `{keyword}`...");
                        }
                        var tableNames = await _databaseInteractor.SearchTablesByNameAsync(keyword);
                        var tableNamesInMarkdown = string.Join(", ", tableNames.Select(x => $"`{x}`"));
                        SetProgressMessage($"{tableNames.Count} tables found:\n\n{tableNamesInMarkdown}");
                        return FunctionCallingHelper.CreateResponse(name, tableNamesInMarkdown);
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.GetUserPermissionsAsync):
                    {
                        SetProgressMessage("Retrieving user permissions...");
                        var permissions = await _databaseInteractor.GetUserPermissionsAsync();
                        var permissionsInMarkdown = string.Join(", ", permissions.Select(x => $"`{x}`"));
                        SetProgressMessage($"{permissions.Count} permissions found:\n\n{permissionsInMarkdown}");
                        return FunctionCallingHelper.CreateResponse(name, permissionsInMarkdown);
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(RequestForActionPlanAsync):
                    {
                        SetProgressMessage("Analyzing the situation and creating an action plan...");
                        var actionPlan = await RequestForActionPlanAsync();
                        SetProgressMessage(actionPlan);
                        return FunctionCallingHelper.CreateResponse(name, actionPlan);
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(RequestForInternetSearchAsync):
                    {
                        var requirement = FunctionCallingHelper.GetParameterValue<string>(function, "query");
                        SetProgressMessage($"Let me perform an in-depth internet search following this description:\n\n{requirement}");
                        var searchResult = await RequestForInternetSearchAsync(requirement);
                        SetProgressMessage(searchResult);
                        return FunctionCallingHelper.CreateResponse(name, searchResult);
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(ChangeTheConversationLanguage):
                    {
                        var language = FunctionCallingHelper.GetParameterValue<string>(function, "language");
                        ChangeTheConversationLanguage(language);
                        return FunctionCallingHelper.CreateResponse(name, $"From now on, AskDB **must use {language}** for the conversation (except for the tool calling).");
                    }
                default: throw new NotImplementedException($"Function '{function.Name}' is not implemented.");
            }
        }
    }
}
