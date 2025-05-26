using AskDB.App.Helpers;
using AskDB.App.ViewModels;
using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Commons.Helpers;
using AskDB.Database.Extensions;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseInteractor.Services;
using DatabaseInteractor.Services.Extractors;
using GeminiDotNET;
using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;
using GeminiDotNET.ApiModels.Enums;
using GeminiDotNET.ApiModels.Response.Success.FunctionCalling;
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
using Windows.Globalization;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

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
            _globalInstruction = _globalInstruction.Replace("{Database_Type}", request.DatabaseType.GetAttributeValue<string>("Description")).Replace("{Language}", "English");

            _extractor = request.DatabaseType switch
            {
                DatabaseType.SqlServer => new SqlServerExtractor(request.ConnectionString),
                DatabaseType.MySQL => new MySqlExtractor(request.ConnectionString),
                DatabaseType.PostgreSQL => new PostgreSqlExtractor(request.ConnectionString),
                DatabaseType.SQLite => new SqliteExtractor(request.ConnectionString),
                _ => throw new NotImplementedException(),
            };

            _generator = new Generator(Cache.ApiKey).EnableChatHistory(50);

            FunctionCallingManager.RegisterFunction(nameof(_extractor.GetUserPermissionsAsync), @"Use this function to retrieve a list of the database permissions currently granted to the application's user session.
This information can be crucial for:
- Understanding why certain operations might be failing due to insufficient privileges.
- Assessing if a requested action is permissible before attempting it.
- Informing the user about their current capabilities within the database if they inquire or if it's relevant to a problem.
The output will describe the user's permissions in the database.");
            FunctionCallingManager.RegisterFunction(nameof(_extractor.GetDatabaseSchemaNamesAsync), @"Use this function to retrieve a list of available schema names within the connected database.
This function is useful when:
- The user refers to a table but doesn't specify a schema, and you need to identify potential schemas.
- You need to present a list of schemas to the user for selection.
- You are trying to fully qualify a table name (schema.table) and need to confirm schema existence.
- The user explicitly asks to list schemas.
You can filter the list using a keyword. If the keyword is an empty string, all accessible schema names will be returned.
The output will be a list of schema names.", new Parameters
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
            FunctionCallingManager.RegisterFunction(nameof(_extractor.GetSchemaInfoAsync), @"Use this function to retrieve structural information for a specific table.
The information returned includes:
- Column names, data types, nullability, and default values.
This tool is CRITICAL for:
- Formulating accurate and safe SQL queries, especially when constructing WHERE clauses or JOIN conditions.
- Understanding data types before attempting INSERT or UPDATE operations.
- Verifying table and column existence before referencing them in queries.
- Helping the user understand their data structure if they ask.
- Assessing potential impacts of queries (e.g., understanding relationships before a DELETE).
If the user does not specify a schema, and the database type has a common default (e.g., `dbo` for SQL Server, `public` for PostgreSQL), you should use that default schema. If unsure about the schema, consider using `GetDatabaseSchemaNamesAsync` function first or asking the user for the clarification.", new Parameters
            {
                Properties = new
                {
                    schema = new
                    {
                        type = "string",
                        description = "The name of the schema containing the table. This is often case-sensitive depending on the database. If not explicitly provided by the user or clear from context, attempt to use the database's default schema (e.g., 'dbo' for SQL Server, 'public' for PostgreSQL). If still uncertain, clarify with the user or use 'GetDatabaseSchemaNamesAsync' to find possible schemas."
                    },
                    table = new
                    {
                        type = "string",
                        description = "The name of the table for which to retrieve detailed schema information. This is often case-sensitive depending on the database."
                    }
                },
                Required = [ "schema", "table"]
            });
            FunctionCallingManager.RegisterFunction("RequestForActionPlan", @"Use this function when you, the AI Agent, encounter a situation where you are unsure how to proceed, have encountered an unexpected error from another tool that you cannot resolve, or believe the user's request requires a sequence of actions that needs higher-level strategic planning beyond simple SQL execution.
This tool signals a need for collaborative problem-solving or guidance.
Specifically, use this when:
- A tool call results in an error you cannot diagnose or fix by simply retrying or slightly modifying parameters (e.g., persistent permission issues, unexpected database state).
- The user's request is very high-level or ambiguous, and initial clarifications haven't led to a concrete, safe series of steps.
- You need to perform a complex task that might involve multiple tool calls and conditional logic, and you require confirmation or a structured approach.
- You assess that the current path is too risky, and you need to escalate for a revised strategy.
You should clearly summarize the current problem or situation when you explain *why* you are calling this tool.", null);
            FunctionCallingManager.RegisterFunction("RequestForInternetSearch", @"Use this function ONLY when you need external information from the internet using Google Search engine to better understand or fulfill a user's database-related request.
This is NOT for general web browsing.
Situations for use include:
- The user asks a question about a feature, error code, or concept that is not within your current knowledge base and cannot be determined through schema inspection or existing tools.
- The user's query implies knowledge of external factors that might influence data interpretation (e.g., 'Find customers affected by the recent policy change announced on [website]').
- You need to understand a specific technical term or standard practice related to the SQL database that is blocking your ability to form a safe and effective plan.
**DO NOT** use this for information readily available through schema tools (like `GetSchemaInfoAsync`) or simple SQL queries.
Always prioritize internal knowledge and database introspection tools first.
When calling, provide a very specific query detailing the information needed and the context.", new Parameters
            {
                Properties = new
                {
                    query = new
                    {
                        type = "string",
                        description = "A highly specific and detailed natural language query for the internet search in markdown format. It MUST include: 1. A brief summarization of the current user request or problem. 2. The specific information you are trying to find. 3. How this information will help you address the user's database-related task. Example: 'User wants to implement versioning for a table in SQL Server. Search for best practices and common patterns for implementing table versioning or temporal tables specifically for {Database_Type}. Expected outcome: Understanding of typical approaches like history tables or system-versioned tables to propose a solution.'"
                    }
                },
                Required = ["query"]
            });
            FunctionCallingManager.RegisterFunction("ChangeTheConversationLanguage", "Force the AI agent to use the specific language for the conversation from now on", new Parameters
            {
                Properties = new
                {
                    language = new
                    {
                        type = "string",
                        description = "The language to change (for example: English, Vietnamese, French, etc)"
                    }
                },
                Required = ["language"]
            });
        }
        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var dataTable = (dataGrid?.DataContext as ProgressContent)?.Data;

            if (dataTable == null)
            {
                return;
            }

            dataGrid.Columns.Clear();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                dataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = dataTable.Columns[i].ColumnName,
                    Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                });
            }

            var collectionObjects = new ObservableCollection<object>(dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray));

            dataGrid.ItemsSource = collectionObjects;
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

            SetMessage(userInput, true);

            try
            {
                await HandleUserInputAsync(userInput);
            }
            catch (Exception ex)
            {
                SetMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
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
        private void SetMessage(string message, bool isFromUser = false)
        {
            var chatMessage = new ChatMessage
            {
                Content = message,
                Alignment = isFromUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            Messages.Add(chatMessage);
        }
        private void SetProgressContent(string message, string sqlCommand, bool isActionButtonVisible, DataTable dataTable = null)
        {
            var progressContent = new ProgressContent
            {
                Message = message,
                SqlCommand = sqlCommand ?? $"Executed SQL command:\n\n```sql\n{sqlCommand}\n```",
                ActionButtonVisibility = VisibilityHelper.SetVisible(isActionButtonVisible),
                Data = dataTable
            };

            ProgressContents.Add(progressContent);
        }
        #endregion

        private async Task HandleUserInputAsync(string userInput)
        {
            var instruction = await FileHelper.ReadFileAsync("Instructions/Global.md");
            instruction = instruction.Replace("{Database_Type}", _extractor.DatabaseType.ToString());
            var functionDeclarations = FunctionCallingManager.FunctionDeclarations;

            var apiRequest = new ApiRequestBuilder()
                .WithSystemInstruction(instruction)
                .WithPrompt(userInput)
                .DisableAllSafetySettings()
                .WithFunctionDeclarations(functionDeclarations, FunctionCallingMode.AUTO)
                .Build();

            var modelResponse = await _generator.GenerateContentAsync(apiRequest, Cache.ReasoningModelAlias);

            var functionCalls = (modelResponse.FunctionCalls == null || modelResponse.FunctionCalls.Count == 0) ? [] : modelResponse.FunctionCalls;

            if (functionCalls.Count == 0)
            {
                SetMessage(modelResponse.Content, false);
                return;
            }

            while (functionCalls.Count > 0)
            {
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
                                    Output = $"{output}\n\nAction plan or clarification is required.",
                                }
                            });
                            SetProgressContent(output, null, false, null);
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
                        SetMessage(modelResponseForFunction.Content, false);
                    }
                    else
                    {
                        SetProgressContent(modelResponseForFunction.Content, null, false, null);
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    SetMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
                    functionCalls = [];
                }
            }
        }

        private async Task<FunctionResponse> CallFunctionAsync(FunctionCall function)
        {
            static FunctionResponse CreateResponse(string name, string output) => new()
            {
                Name = name,
                Response = new Response { Output = output }
            };

            switch (function.Name)
            {
                case var name when name == FunctionCallingManager.ExecuteQueryAsyncFunction.Name:
                    {
                        var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        var dataTable = await _extractor.ExecuteQueryAsync(sqlQuery);
                        SetProgressContent("Let me take a look into this data", sqlQuery, true, dataTable);
                        return CreateResponse(FunctionCallingManager.ExecuteQueryAsyncFunction.Name, dataTable.ToMarkdown());
                    }
                case var name when name == FunctionCallingManager.ExecuteNonQueryAsyncFunction.Name:
                    {
                        var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        await _extractor.ExecuteNonQueryAsync(sqlQuery);
                        SetProgressContent("Command executed successfully.", null, false, null);
                        return CreateResponse(FunctionCallingManager.ExecuteNonQueryAsyncFunction.Name, $"Command executed successfully.\n\n```sql\n{sqlQuery}\n```");
                    }
                case var name when name == nameof(_extractor.GetSchemaInfoAsync):
                    {
                        var schema = FunctionCallingHelper.GetParameterValue<string>(function, "schema");
                        var table = FunctionCallingHelper.GetParameterValue<string>(function, "table");
                        SetProgressContent($"Let me check the schema information for the table`{schema}.{table}`", null, false, null);
                        var schemaInfo = await _extractor.GetSchemaInfoAsync(table, schema);
                        SetProgressContent("Let me take a look into this data", null, false, schemaInfo);
                        return CreateResponse(nameof(_extractor.GetSchemaInfoAsync), schemaInfo.ToMarkdown());
                    }
                case var name when name == nameof(_extractor.GetDatabaseSchemaNamesAsync):
                    {
                        var keyword = FunctionCallingHelper.GetParameterValue<string>(function, "keyword");
                        SetProgressContent($"Let me check the database schema names with the keyword `{keyword}`", null, false, null);
                        var schemaNames = await _extractor.GetDatabaseSchemaNamesAsync(keyword);
                        var schemaNamesInMarkdown = string.Join(", ", schemaNames.Select(x => $"`{x}`"));
                        SetProgressContent($"I found these schemas: {schemaNamesInMarkdown}", null, false, null);
                        return CreateResponse(nameof(_extractor.GetDatabaseSchemaNamesAsync), schemaNamesInMarkdown);
                    }
                case var name when name == nameof(_extractor.GetUserPermissionsAsync):
                    {
                        var permissions = await _extractor.GetUserPermissionsAsync();
                        var permissionsInMarkdown = string.Join(", ", permissions.Select(x => $"`{x}`"));
                        SetProgressContent($"Found these permissions: {permissionsInMarkdown}", null, false, null);
                        return CreateResponse(nameof(_extractor.GetUserPermissionsAsync), permissionsInMarkdown);
                    }
                case "RequestForActionPlan":
                    {
                        var actionPlanRequest = new ApiRequestBuilder()
                            .WithSystemInstruction(_globalInstruction)
                            .DisableAllSafetySettings()
                            .WithFunctionDeclarations(FunctionCallingManager.FunctionDeclarations, FunctionCallingMode.NONE)
                            .WithPrompt("I have some blocking points here and need you to make an action plan to overcome. Now think deeply step-by-step about the current situation, then provide a clear and detailed action plan.")
                            .Build();

                        var actionPlanResponse = await _generator.GenerateContentAsync(actionPlanRequest, Cache.ReasoningModelAlias);
                        SetMessage(actionPlanResponse.Content, false);
                        return CreateResponse("RequestForActionPlan", actionPlanResponse.Content);
                    }
                case "RequestForInternetSearch":
                    {
                        var query = FunctionCallingHelper.GetParameterValue<string>(function, "query");
                        var searchRequest = new ApiRequestBuilder()
                            .WithSystemInstruction(_globalInstruction)
                            .EnableGrounding()
                            .DisableAllSafetySettings()
                            .WithPrompt($"Now do an **in-depth internet research**, then provide a detailed reported in markdown format. The search result MUST satify the below description:\n\n{query}")
                            .Build();
                        var generator = new Generator(Cache.ApiKey);
                        var searchResponse = await generator.GenerateContentAsync(searchRequest, ModelVersion.Gemini_20_Flash);
                        SetProgressContent(searchResponse.Content, null, false, null);
                        return CreateResponse("RequestForInternetSearch", searchResponse.Content);
                    }
                case "ChangeTheConversationLanguage":
                    {
                        var language = FunctionCallingHelper.GetParameterValue<string>(function, "language");
                        _globalInstruction = _globalInstruction.Replace("{Language}", language);
                        SetMessage($"From now on, AskDb will talk with you using {language}.", false);
                        return CreateResponse("ChangeTheConversationLanguage", $"From now on, the agent **must use {language}** for the conversation (except for the tool calling).");
                    }
                default:
                    return null;
            }
        }
    }
}
