using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.Commons.Extensions;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseInteractor.Factories;
using DatabaseInteractor.Helpers;
using DatabaseInteractor.Services;
using GeminiDotNET;
using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;
using GeminiDotNET.ApiModels.Enums;
using GeminiDotNET.ApiModels.Response.Success;
using GeminiDotNET.ApiModels.Response.Success.FunctionCalling;
using GeminiDotNET.FunctionCallings.Attributes;
using GeminiDotNET.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        private readonly ObservableCollection<AgentSuggestion> AgentSuggestions = [];

        private Generator _generator;
        private DatabaseInteractionService _databaseInteractor;
        private string _globalInstruction;
        private string _actionPlanInstruction;

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
                _databaseInteractor = ServiceFactory.CreateInteractionService(request.DatabaseType, request.ConnectionString);
                _generator = new Generator(Cache.ApiKey).EnableChatHistory(150);

                try
                {
                    var globalTask = OnlineContentHelper.GetSytemInstructionContentAsync("Global", _databaseInteractor.DatabaseType, "English");
                    var actionPlanTask = OnlineContentHelper.GetSytemInstructionContentAsync("Action Plan", _databaseInteractor.DatabaseType, "English");
                    var initQueryTemplatesTask = _databaseInteractor.InitQueryTemplatesAsync();

                    await Task.WhenAll(globalTask, actionPlanTask, initQueryTemplatesTask);

                    _globalInstruction = globalTask.Result;
                    _actionPlanInstruction = actionPlanTask.Result;

                    ChangeTheConversationLanguage("English");
                    InitFunctionCalling();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot load system instructions: {ex.Message}", ex);
                }

                try
                {
                    var tableNames = await LoadTableNamesAsync();

                    if (tableNames.Count > 0)
                    {
                        await LoadAgentSuggestionsAsync($@"Based on the list of table names provided below, suggest me with 5 concise and interesting ideas from my viewpoint that I might ask to start the conversation.
The suggested ideas should be phrased as questions or commands in natural language, assuming I am not technical but want to understand and explore the data for analysis purpose or predictional purpose.
Focus on common exploratory intents such as: viewing recent records, counting items, finding top or recent entries, understanding relationships, checking for missing/empty data, searching for specific information, or exploring the table structure, etc.
Avoid SQL or technical jargon in the suggestions. Each suggestion should be unique, short, concise, specific, user-friendly, and **MUST NOT** be relevant to sensitive, security-related, or credential-related tables, and do not suggest actions that require elevated permissions or could lead to data loss or sensitive information exposure.

This is the list of table names in the database: {string.Join(", ", tableNames.Select(x => $"`{x}`"))}");
                    }

                    var requestForAgentSuggestions = new ApiRequestBuilder()
                       .WithSystemInstruction(_globalInstruction)
                       .DisableAllSafetySettings()
                       .WithDefaultGenerationConfig(1.5F, 350)
                       .WithPrompt("In order to start the conversation, please introduce to me about yourself, such as who you are, what you can do, what you can help me, or anything else that you think it may be relavant to my database and be useful to me; and some good practices for me to help you to do the task effectively. Take me as your friend or your teammate, avoid to use formal-like tone while talking to me; just use a natural, friendly tone with daily-life word when talking to me, like you are talking with your friends in the real life.")
                       .Build();

                    var response = await _generator.GenerateContentAsync(requestForAgentSuggestions, ModelVersion.Gemini_20_Flash_Lite);

                    if (!string.IsNullOrEmpty(response.Content))
                    {
                        SetAgentMessage(response.Content);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot load table names. {ex.Message}", ex);
                }
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
                DataTable? dataTable = ((sender as Button)?.DataContext as ChatMessage)?.Data;

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

            if (dataGrid == null || dataGrid?.DataContext is not ChatMessage context || (context.Data == null || context.Data.Rows.Count == 0))
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
            var chatMessage = new ChatMessage
            {
                Message = message,
                IsFromUser = true,
                IsFromAgent = false,
            };

            Messages.Add(chatMessage);
        }

        private void SetAgentMessage(string? message, DataTable? dataTable = null)
        {
            var isDataTableEmpty = dataTable == null || dataTable.Rows.Count == 0;

            var chatMessage = new ChatMessage
            {
                Message = string.IsNullOrEmpty(message) ? string.Empty : message,
                IsFromUser = false,
                IsFromAgent = true,
                QueryResults = isDataTableEmpty ? [] : new ObservableCollection<object>(dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray)),
                Data = isDataTableEmpty ? null : dataTable
            };

            Messages.Add(chatMessage);
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
                        description = "A keyword to filter the table names. If an empty string is provided, all accessible table names with their associated schema are returned."
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
                        description = "The name of the schema containing the table. This is often case-sensitive depending on the database. **Be noted** that schema parameter is *only* supported for SQL Server and PostgreSQL database, otherwise, leave it empty or null value. In addition, if the schema name is not explicitly provided by the user or clear from context, just ignore it. "
                    },
                    table = new
                    {
                        type = "string",
                        description = "The exact name of the table for which to retrieve detailed schema information. This is often case-sensitive depending on the database."
                    }
                },
                Required = ["table"]
            });
        }
        #endregion

        private async Task HandleUserInputAsync(string userInput)
        {
            try
            {
                SetLoading(true, "Working");
                SetUserMessage(userInput);

                AgentSuggestionsItemView.Visibility = VisibilityHelper.SetVisible(false);
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
                if (!string.IsNullOrEmpty(modelResponse.Content))
                {
                    SetAgentMessage(modelResponse.Content);
                }

                var functionCalls = (modelResponse.FunctionCalls == null || modelResponse.FunctionCalls.Count == 0) ? [] : modelResponse.FunctionCalls;

                if (string.IsNullOrEmpty(modelResponse.Content) && functionCalls.Count == 0)
                {
                    var failedResonse = JsonHelper.AsObject<ApiResponse>(_generator.ResponseAsRawString);
                    var finishReason = failedResonse?.Candidates.FirstOrDefault()?.FinishReason;
                    _generator.ResponseAsRawString.CopyToClipboard();

                    if (!string.IsNullOrEmpty(finishReason))
                    {
                        SetAgentMessage($"AskDB refused to answer! The reason code name is [{finishReason.Replace('_', ' ')}](https://cloud.google.com/vertex-ai/docs/reference/rest/v1/GenerateContentResponse#FinishReason).");
                    }
                }

                var loopAttempt = 0;
                while (functionCalls.Count > 0 && loopAttempt <= 10)
                {
                    loopAttempt++;
                    await Task.Delay(2345);
                    
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
                                var output = $"Error in function **{function.Name}**:\n\n```console\n{ex.Message}. {ex.InnerException?.Message}\n```";

                                functionResponses.Add(new FunctionResponse
                                {
                                    Name = function.Name,
                                    Response = new Response
                                    {
                                        Output = $"{output}\n\nAction plan or user clarification or internet search is required.",
                                    }
                                });

                                SetAgentMessage(output);
                            }
                            finally
                            {
                                await Task.Delay(543);
                            }
                        }

                        var apiRequestWithFunctions = new ApiRequestBuilder()
                            .WithSystemInstruction(_globalInstruction)
                            .DisableAllSafetySettings()
                            .WithTools(new ToolBuilder().AddFunctionDeclarations(functionDeclarations))
                            .SetFunctionCallingMode(FunctionCallingMode.AUTO)
                            .WithFunctionResponses(functionResponses)
                            .WithPrompt(@"Please review the function responses and your action history against the user's initial request in order to take action: continue on function calling until the request is satified, or reply to the user immediately to provide the final answer or ask for user's clarification!")
                            .Build();

                        var modelResponseForFunction = await _generator.GenerateContentAsync(apiRequestWithFunctions, ModelVersion.Gemini_20_Flash);
                        SetAgentMessage(modelResponseForFunction.Content);

                        functionCalls = (modelResponseForFunction.FunctionCalls == null || modelResponseForFunction.FunctionCalls.Count == 0) ? [] : modelResponseForFunction.FunctionCalls;
                    }
                    catch (Exception ex)
                    {
                        SetAgentMessage($"Error: {ex.Message}. {ex.InnerException?.Message}");
                        functionCalls = [];
                    }
                    finally
                    {
                        await Task.Delay(234);
                    }
                }

                if (loopAttempt < 10)
                {
                    await LoadAgentSuggestionsAsync("Based on the current context, please provide up to 5 **short** and **concise** suggestions as the next step in my viewpoint, for me to use as the quick reply in order to continue the task.");
                }
                else
                {
                    await LoadAgentSuggestionsAsync("You have tried with 10 actions but cannot satisfy the task, please provide up to 5 **short** and **concise** suggestions as the next step in my viewpoint, for me to use as the quick reply in order to continue the task.");
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
        private async Task<FunctionResponse> CallFunctionAsync(FunctionCall function)
        {
            switch (function.Name)
            {
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.ExecuteQueryAsync):
                    {
                        var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        SetAgentMessage($"Let me execute this query to check the data:\n\n```sql\n{sqlQuery}\n```");
                        try
                        {
                            var dataTable = await _databaseInteractor.ExecuteQueryAsync(sqlQuery);
                            if (dataTable != null && dataTable.Rows.Count > 1)
                            {
                                SetAgentMessage(null, dataTable);
                                return FunctionCallingHelper.CreateResponse(name, dataTable.ToMarkdown());
                            }
                            else
                            {
                                return FunctionCallingHelper.CreateResponse(name, "No data found for the query.");
                            }
                        }
                        catch (Exception ex)
                        {
                            SetAgentMessage($"Error while executing your query.\n\n```console\n{ex.Message}\n```");
                            return FunctionCallingHelper.CreateResponse(name, $"Error while executing your {_databaseInteractor.DatabaseType.GetDescription()} query.\n\n```console\n{ex.Message}\n```\n\nMake sure that you understand the SQL query clearly before executing it! If the SQL query is too complex, try to break it down first, follow the *KISS (Keep It Simple, Stupid)* principles");
                        }
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.ExecuteNonQueryAsync):
                    {
                        var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        SetAgentMessage($"Let me execute the command:\n\n```sql\n{sqlQuery}\n```");
                        try
                        {
                            await _databaseInteractor.ExecuteNonQueryAsync(sqlQuery);
                            return FunctionCallingHelper.CreateResponse(name, $"Command executed successfully.\n\n```sql\n{sqlQuery}\n```");
                        }
                        catch (Exception ex)
                        {
                            SetAgentMessage($"Error while executing your SQL command.\n\n```console\n{ex.Message}\n```");
                            return FunctionCallingHelper.CreateResponse(name, $"Error while executing your {_databaseInteractor.DatabaseType.GetDescription()} command.\n\n```console\n{ex.Message}\n```\n\nMake sure that you understand the SQL command clearly before executing it! If the SQL command is too complex, try to break it down first, follow the *KISS (Keep It Simple, Stupid)* principles.");
                        }
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.GetTableStructureDetailAsync):
                    {
                        var schema = FunctionCallingHelper.GetParameterValue<string>(function, "schema");
                        var table = FunctionCallingHelper.GetParameterValue<string>(function, "table");
                        SetAgentMessage($"Let me check the schema information for the table `{schema}.{table}`");

                        try
                        {
                            var dataTable = await _databaseInteractor.GetTableStructureDetailAsync(schema, table);

                            if (dataTable != null && dataTable.Rows.Count > 1)
                            {
                                SetAgentMessage(null, dataTable);
                                return FunctionCallingHelper.CreateResponse(name, dataTable.ToMarkdown());
                            }
                            else
                            {
                                return FunctionCallingHelper.CreateResponse(name, "Invalid table or table not found.");
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.CopyToClipboard();
                            SetAgentMessage($"Error while getting table structure.\n\n```console\n{ex.Message}\n```");
                            return FunctionCallingHelper.CreateResponse(name, $"The table does not exist or error while retrieving the table structure.\n\n```console\n{ex.Message}\n```\n\nPlease try searching for the table first by using the `{FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.SearchTablesByNameAsync)} function`.");
                        }
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.SearchTablesByNameAsync):
                    {
                        var keyword = FunctionCallingHelper.GetParameterValue<string>(function, "keyword");
                        if (string.IsNullOrEmpty(keyword))
                        {
                            SetAgentMessage("Let me retrieving all accessible tables.");
                        }
                        else
                        {
                            SetAgentMessage($"Let me search for tables using keyword `{keyword}`.");
                        }
                        var tableNames = await _databaseInteractor.SearchTablesByNameAsync(keyword);

                        if (tableNames.Count > 0)
                        {
                            var tableNamesInMarkdown = string.Join(", ", tableNames.Select(x => $"`{x}`"));
                            SetAgentMessage($"I found these tables: {tableNamesInMarkdown}");
                            return FunctionCallingHelper.CreateResponse(name, tableNamesInMarkdown);
                        }

                        return FunctionCallingHelper.CreateResponse(name, "No tables found. Please try another approach!");
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(_databaseInteractor.GetUserPermissionsAsync):
                    {
                        var permissions = await _databaseInteractor.GetUserPermissionsAsync();
                        var permissionsInMarkdown = string.Join(", ", permissions.Select(x => $"`{x}`"));
                        return FunctionCallingHelper.CreateResponse(name, permissionsInMarkdown);
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(RequestForActionPlanAsync):
                    {
                        SetAgentMessage("Let me analyze the current situation and create an action plan.");
                        var actionPlan = await RequestForActionPlanAsync();
                        SetAgentMessage(actionPlan);
                        return FunctionCallingHelper.CreateResponse(name, actionPlan);
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(RequestForInternetSearchAsync):
                    {
                        var requirement = FunctionCallingHelper.GetParameterValue<string>(function, "requirement");
                        SetAgentMessage($"Let me perform an in-depth internet search following this description:\n\n{requirement}");
                        var searchResult = await RequestForInternetSearchAsync(requirement!);
                        SetAgentMessage(searchResult);
                        return FunctionCallingHelper.CreateResponse(name, searchResult);
                    }
                case var name when name == FunctionDeclarationHelper.GetFunctionName(ChangeTheConversationLanguage):
                    {
                        var language = FunctionCallingHelper.GetParameterValue<string>(function, "language");
                        ChangeTheConversationLanguage(language!);
                        return FunctionCallingHelper.CreateResponse(name, $"From now on, AskDB **must use {language}** for the conversation (except for the tool calling).");
                    }
                default: throw new NotImplementedException($"Function '{function.Name}' is not implemented.");
            }
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var message = ((sender as Button)?.DataContext as ChatMessage)?.Message;

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

        private async Task LoadAgentSuggestionsAsync(string prompt)
        {
            if (AgentSuggestions.Count > 0) AgentSuggestions.Clear();

            try
            {
                var requestForAgentSuggestions = new ApiRequestBuilder()
                   .WithSystemInstruction(_globalInstruction)
                   .DisableAllSafetySettings()
                   .WithDefaultGenerationConfig(0.7F, 250)
                   .WithResponseSchema(new
                   {
                       type = "object",
                       properties = new
                       {
                           UserResponseSuggestions = new
                           {
                               type = "array",
                               items = new
                               {
                                   type = "string"
                               }
                           }
                       },
                       required = new[] { "UserResponseSuggestions" }
                   })
                   .WithPrompt(prompt)
                   .Build();

                var response = await _generator.GenerateContentAsync(requestForAgentSuggestions, ModelVersion.Gemini_20_Flash_Lite);

                if (string.IsNullOrEmpty(response.Content))
                {
                    return;
                }

                var agentResponse = JsonHelper.AsObject<AgentResponse>(response.Content);

                if (agentResponse?.UserResponseSuggestions != null && agentResponse.UserResponseSuggestions.Count > 0)
                {
                    var suggestions = agentResponse.UserResponseSuggestions.Select(suggestion => new AgentSuggestion
                    {
                        UserResponseSuggestion = suggestion
                    });

                    foreach (var suggestion in suggestions)
                    {
                        AgentSuggestions.Add(suggestion);
                    }

                    AgentSuggestionsItemView.Visibility = VisibilityHelper.SetVisible(true);
                }
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                await ShowInforBarAsync($"Cannot load suggestions: {ex.Message}", false);
            }
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
    }
}
