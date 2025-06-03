using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Commons.Helpers;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseInteractor.Function_Callings.Attributes;
using DatabaseInteractor.Services;
using DatabaseInteractor.Services.Extractors;
using GeminiDotNET;
using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;
using GeminiDotNET.ApiModels.Enums;
using GeminiDotNET.ApiModels.Response.Success.FunctionCalling;
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
using FunctionCallingHelper = DatabaseInteractor.FunctionCallings.Services.FunctionCallingHelper;

namespace AskDB.App.Pages
{
    public sealed partial class ChatWithDatabase : Page
    {
        private readonly ObservableCollection<ChatMessage> Messages = [];
        private readonly ObservableCollection<ProgressContent> ProgressContents = [];

        private Generator _generator;
        private ExtractorBase _extractor;
        private string _globalInstruction;

        public ChatWithDatabase()
        {
            this.InitializeComponent();
            SetLoading(false);
        }

        #region Event Handlers
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not DatabaseConnectionInfo request)
            {
                return;
            }

            _globalInstruction = await FileHelper.ReadFileAsync("Instructions/Global.md");
            ChangeTheConversationLanguage("English");

            _extractor = request.DatabaseType switch
            {
                DatabaseType.SqlServer => new SqlServerExtractor(request.ConnectionString),
                DatabaseType.MySQL => new MySqlExtractor(request.ConnectionString),
                DatabaseType.PostgreSQL => new PostgreSqlExtractor(request.ConnectionString),
                DatabaseType.SQLite => new SqliteExtractor(request.ConnectionString),
                _ => throw new NotImplementedException(),
            };

            _generator = new Generator(Cache.ApiKey).EnableChatHistory(50);

            InitFunctionCalling();
        }
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var dataTable = (button?.DataContext as ProgressContent)?.Data;

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

            SetLoading(true);
            SetUserMessage(userInput, true);

            try
            {
                await HandleUserInputAsync(userInput);
            }
            catch (Exception ex)
            {
                SetUserMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
            }
            finally
            {
                SetLoading(false);
            }
        }
        private void QueryBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }
        #endregion

        #region Message and Progress Content Bindings
        private void SetLoading(bool isLoading)
        {
            LoadingIndicator.SetLoading("Analyzing", isLoading);
            LoadingOverlay.Visibility = VisibilityHelper.SetVisible(isLoading);
            MessageSpace.Opacity = isLoading ? 0.2 : 1;
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
                ActionButtonVisibility = VisibilityHelper.SetVisible(false),
            };

            ProgressContents.Add(progressContent);
        }
        private void SetProgressDataTable(string? sqlCommand, DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return;
            }

            var dataGrid = new DataGrid();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                dataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = dataTable.Columns[i].ColumnName,
                    Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                });
            }

            var progressContent = new ProgressContent
            {
                Message = null,
                SqlCommand = sqlCommand,
                ActionButtonVisibility = VisibilityHelper.SetVisible(dataTable.Rows.Count > 0),
                QueryResults = new ObservableCollection<object>(dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray)),
            };

            ProgressContents.Add(progressContent);
        }

        private void InitFunctionCalling()
        {
            FunctionCallingHelper.RegisterFunction(RequestForActionPlanAsync);
            FunctionCallingHelper.RegisterFunction(RequestForInternetSearchAsync, new Parameters
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
            FunctionCallingHelper.RegisterFunction(ChangeTheConversationLanguage, new Parameters
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

            FunctionCallingHelper.RegisterFunction(_extractor.ExecuteQueryAsync, new Parameters
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
            FunctionCallingHelper.RegisterFunction(_extractor.ExecuteNonQueryAsync, new Parameters
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
            FunctionCallingHelper.RegisterFunction(_extractor.GetUserPermissionsAsync);
            FunctionCallingHelper.RegisterFunction(_extractor.SearchSchemasByNameAsync, new Parameters
            {
                Properties = new
                {
                    keyword = new
                    {
                        type = "string",
                        description = "A keyword to filter the schema names. For example, `prod` might return `Production_schema`, `prod_data`. If an empty string is provided, all accessible schema names are returned. The search is typically case-insensitive and looks for the keyword within the schema names."
                    }
                },
                Required = ["keyword"]
            });
            FunctionCallingHelper.RegisterFunction(_extractor.SearchTablesByNameAsync, new Parameters
            {
                Properties = new
                {
                    schema = new
                    {
                        type = "string",
                        description = "The name of the schema containing the table. This is often case-sensitive depending on the database. If not explicitly provided by the user or clear from context, attempt to use the database's default schema (e.g., 'dbo' for SQL Server, 'public' for PostgreSQL). If still uncertain, clarify with the user or use 'SearchSchemasByNameAsync' to find possible schemas."
                    },
                    keyword = new
                    {
                        type = "string",
                        description = "A keyword to filter the table names. For example, `user` might return `Users`, `UserAccounts`. If an empty string is provided, all accessible table names in the specified schema are returned. The search is typically case-insensitive and looks for the keyword within the table names."
                    }
                },
                Required = ["schema"]
            });
            FunctionCallingHelper.RegisterFunction(_extractor.GetTableStructureDetailAsync, new Parameters
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
            var instruction = await FileHelper.ReadFileAsync("Instructions/Global.md");
            instruction = instruction.Replace("{Database_Type}", _extractor.DatabaseType.ToString());
            var functionDeclarations = FunctionCallingHelper.FunctionDeclarations;

            var apiRequest = new ApiRequestBuilder()
                .WithSystemInstruction(instruction)
                .WithPrompt(userInput)
                .DisableAllSafetySettings()
                .WithFunctionDeclarations(functionDeclarations, FunctionCallingMode.AUTO)
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
                        .WithSystemInstruction(instruction)
                        .DisableAllSafetySettings()
                        .WithFunctionDeclarations(functionDeclarations, FunctionCallingMode.AUTO)
                        .WithFunctionResponses(functionResponses)
                        .Build();

                    var modelResponseForFunction = await _generator.GenerateContentAsync(apiRequestWithFunctions, ModelVersion.Gemini_20_Flash_Lite);

                    functionCalls = (modelResponseForFunction.FunctionCalls == null || modelResponseForFunction.FunctionCalls.Count == 0) ? [] : modelResponseForFunction.FunctionCalls;

                    if (functionCalls.Count == 0)
                    {
                        SetUserMessage(modelResponseForFunction.Content, false);
                    }
                    else
                    {
                        SetProgressMessage(modelResponseForFunction.Content);
                    }
                }
                catch (Exception ex)
                {
                    SetUserMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
                    functionCalls = [];
                }
            }
        }

        private async Task<FunctionResponse> CallFunctionAsync(FunctionCall function)
        {
            switch (function.Name)
            {
                case var name when name == FunctionCallingHelper.GetFunctionName(_extractor.ExecuteQueryAsync):
                    {
                        var sqlQuery = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        SetProgressMessage($"Let me execute this query to check the data:\n\n```sql\n{sqlQuery}\n```");
                        var dataTable = await _extractor.ExecuteQueryAsync(sqlQuery);
                        SetProgressDataTable(sqlQuery, dataTable);
                        return FunctionCallingHelper.CreateResponse(name, dataTable.ToMarkdown());
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(_extractor.ExecuteNonQueryAsync):
                    {
                        var sqlQuery = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        SetProgressMessage($"Let me execute the command:\n\n```sql\n{sqlQuery}\n```");
                        await _extractor.ExecuteNonQueryAsync(sqlQuery);
                        SetProgressMessage("Command executed successfully.");
                        return FunctionCallingHelper.CreateResponse(name, $"Command executed successfully.\n\n```sql\n{sqlQuery}\n```");
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(_extractor.GetTableStructureDetailAsync):
                    {
                        var schema = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "schema");
                        var table = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "table");
                        SetProgressMessage($"Let me check the schema information for the table `{schema}.{table}`");
                        var schemaInfo = await _extractor.GetTableStructureDetailAsync(schema, table);
                        SetProgressDataTable(null, schemaInfo);
                        return FunctionCallingHelper.CreateResponse(name, schemaInfo.ToMarkdown());
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(_extractor.SearchSchemasByNameAsync):
                    {
                        var keyword = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "keyword");
                        SetProgressMessage($"Searching for schemas with keyword `{keyword}`...");
                        var schemaNames = await _extractor.SearchSchemasByNameAsync(keyword);
                        var schemaNamesInMarkdown = string.Join(", ", schemaNames.Select(x => $"`{x}`"));
                        SetProgressMessage($"Found {schemaNames.Count} schemas with keyword `{keyword}`:\n\n{schemaNamesInMarkdown}");
                        return FunctionCallingHelper.CreateResponse(name, schemaNamesInMarkdown);
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(_extractor.SearchTablesByNameAsync):
                    {
                        var schema = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "schema");
                        var keyword = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "keyword");
                        SetProgressMessage($"Searching for tables in schema `{schema}` with keyword `{keyword}`...");
                        var tableNames = await _extractor.SearchTablesByNameAsync(schema, keyword);
                        var tableNamesInMarkdown = string.Join(", ", tableNames.Select(x => $"`{x}`"));
                        SetProgressMessage($"Found {tableNames.Count} tables in schema `{schema}` with keyword `{keyword}`:\n\n{tableNamesInMarkdown}");
                        return FunctionCallingHelper.CreateResponse(name, tableNamesInMarkdown);
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(_extractor.GetUserPermissionsAsync):
                    {
                        SetProgressMessage("Retrieving user permissions...");
                        var permissions = await _extractor.GetUserPermissionsAsync();
                        var permissionsInMarkdown = string.Join(", ", permissions.Select(x => $"`{x}`"));
                        SetProgressMessage($"Found {permissions.Count} permissions:\n\n{permissionsInMarkdown}");
                        return FunctionCallingHelper.CreateResponse(name, permissionsInMarkdown);
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(RequestForActionPlanAsync):
                    {
                        SetProgressMessage("Analyzing the situation and creating an action plan...");
                        var actionPlan = await RequestForActionPlanAsync();
                        SetProgressMessage(actionPlan);
                        return FunctionCallingHelper.CreateResponse(name, actionPlan);
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(RequestForInternetSearchAsync):
                    {
                        var requirement = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "query");
                        SetProgressMessage($"Let me perform an in-depth internet search following this description:\n\n{requirement}");
                        var searchResult = await RequestForInternetSearchAsync(requirement);
                        SetProgressMessage(searchResult);
                        return FunctionCallingHelper.CreateResponse(name, searchResult);
                    }
                case var name when name == FunctionCallingHelper.GetFunctionName(ChangeTheConversationLanguage):
                    {
                        var language = GeminiDotNET.Helpers.FunctionCallingHelper.GetParameterValue<string>(function, "language");
                        ChangeTheConversationLanguage(language);
                        return FunctionCallingHelper.CreateResponse(name, $"From now on, the agent **must use {language}** for the conversation (except for the tool calling).");
                    }
                default: throw new NotImplementedException($"Function '{function.Name}' is not implemented.");
            }
        }

        private async void CopySqlButton_Click(object sender, RoutedEventArgs e)
        {
            var context = (sender as Button)?.DataContext as ProgressContent;

            if (context == null || string.IsNullOrEmpty(context.SqlCommand))
            {
                return;
            }

            var package = new DataPackage();
            package.SetText(context.SqlCommand);
            Clipboard.SetContent(package);

            await DialogHelper.ShowSuccessAsync("SQL command has been copied to clipboard.");
        }

        [FunctionDeclaration("change_the_conversation_language", "Change the conversation language for the agent. This will update the global instruction to reflect the new language setting.")]
        private void ChangeTheConversationLanguage(string language)
        {
            _globalInstruction = _globalInstruction
                .Replace("{Language}", language)
                .Replace("{DateTime_Now}", DateTime.Now.ToString("HH:mm:ss, mm.MM.yyyy"));
        }

        [FunctionDeclaration("request_for_action_plan", @"Request an action plan based on the current situation. This will analyze the current situation and generate a detailed action plan.
Use this function when you encounter a situation where you are unsure how to proceed, have encountered an unexpected error from another tool that you cannot resolve, or believe the user's request requires a sequence of actions that needs higher-level strategic planning beyond simple SQL execution.
This tool signals a need for collaborative problem-solving or guidance.

Specifically, use this when:
- A tool call results in an error you cannot diagnose or fix by simply retrying or slightly modifying parameters (e.g., persistent permission issues, unexpected database state).
- You believe the user's request requires a sequence of actions that needs higher-level strategic planning beyond simple SQL execution.
- It is a signal that you need to escalate the problem for collaborative problem-solving or guidance.
- It is not intended for direct user interaction but rather for internal use to clarify the current state or request further assistance.
- It is a call to seek further assistance if the current approach is insufficient or unclear.
- It is a signal that the current path is too risky, and you need to escalate for a revised strategy.
- The user's request is very high-level or ambiguous, and initial clarifications haven't led to a concrete, safe series of steps.
- You need to perform a complex task that might involve multiple tool calls and conditional logic, and you require confirmation or a structured approach.")]
        private async Task<string> RequestForActionPlanAsync()
        {
            var instruction = await FileHelper.ReadFileAsync("Instructions /ActionPlan.md");

            var actionPlanRequest = new ApiRequestBuilder()
                .WithSystemInstruction(instruction.Replace("{Database_Type}", _extractor.DatabaseType.ToString()))
                .DisableAllSafetySettings()
                .WithFunctionDeclarations(FunctionCallingHelper.FunctionDeclarations, FunctionCallingMode.NONE)
                .WithPrompt("I have some blocking points here and need you to make an action plan to overcome. Now think deeply step-by-step about the current situation, then provide a clear and detailed action plan.")
                .Build();

            var actionPlanResponse = await _generator.GenerateContentAsync(actionPlanRequest, Cache.ReasoningModelAlias);

            return actionPlanResponse.Content;
        }

        [FunctionDeclaration("request_for_internet_search", @"Perform an in-depth internet search based on the provided query. Use this function ONLY when you need external information from the internet using Google Search engine to better understand or fulfill a user's database-related request.
This is NOT for general web browsing.

Situations for use include:
- The user asks a question about a feature, error code, or concept that is not within your current knowledge base and cannot be determined through schema inspection or existing tools.
- The user's query implies knowledge of external factors that might influence data interpretation (e.g., 'Find customers affected by the recent policy change announced on [website]').
- You need to understand a specific technical term or standard practice related to the SQL database that is blocking your ability to form a safe and effective plan.
- You want to gather additional context or best practices from external sources to enhance your understanding of the user's request.
- You need to find information that is not directly related to the database schema or data but is relevant to the user's request (e.g., industry standards, recent updates, etc.).
- You are trying to find solutions to common problems or errors that are not specific to the database schema but are relevant to the user's request.
- You need to search for best practices or common patterns related to the user's request that are not specific to the database schema but are relevant to the user's request.
- You need to search for the solution for dealing with a specific error code or message that is not directly related to the database schema but is relevant to the user's request.
- You might want to look for documentation that relates to the current database version or features that are not covered by the schema tools.
- You want to search for the SQL syntax or examples that are not directly related to the database schema but are relevant to the user's request.
- You might want to search for the SQL performance tuning tips or optimization techniques that can enhance query efficiency and effectiveness.
- You might want to search for external insights or recent trends that could inform your query strategy.
- You want to search for best practices for authoring complex SQL queries that can optimize execution time and resource consumption.

Important notes:
- **DO NOT** use this for information readily available through schema tools or simple SQL queries.
- Always prioritize internal knowledge and database introspection tools first.
- When calling, provide a very specific query detailing the information needed and the context.")]
        private async Task<string> RequestForInternetSearchAsync(string requirement)
        {
            var searchRequest = new ApiRequestBuilder()
                .WithSystemInstruction(_globalInstruction)
                .EnableGrounding()
                .WithDefaultGenerationConfig(0.5F)
                .DisableAllSafetySettings()
                .WithPrompt($"Now perform an **in-depth internet research**, then provide a detailed reported in markdown format. The search result MUST satify the below requirement:\n\n{requirement}")
                .Build();

            var searchResponse = await _generator.GenerateContentAsync(searchRequest, ModelVersion.Gemini_20_Flash);
            return searchResponse.Content;

        }
    }
}
