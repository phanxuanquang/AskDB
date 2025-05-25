using AskDB.App.Helpers;
using AskDB.App.View_Models;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseInteractor.Helper;
using DatabaseInteractor.Models.Enums;
using DatabaseInteractor.Services;
using DatabaseInteractor.Services.Extractors;
using GeminiDotNET;
using GeminiDotNET.ApiModels.Enums;
using GeminiDotNET.ApiModels.Response.Success.FunctionCalling;
using GeminiDotNET.Helpers;
using Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.BadgeNotifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AskDB.App.Pages
{
    public sealed partial class ChatWithDatabase : Page
    {
        private ObservableCollection<ChatMessage> Messages = [];
        private ObservableCollection<ProgressContent> ProgressContents = [];

        private string _userInputText = string.Empty;
        private Generator _generator;
        private ExtractorBase _extractor;

        public ChatWithDatabase()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not DatabaseConnectionInfo request)
            {
                return;
            }

            _extractor = request.DatabaseType switch
            {
                DatabaseType.SqlServer => new SqlServerExtractor(request.ConnectionString),
                DatabaseType.MySQL => new MySqlExtractor(request.ConnectionString),
                DatabaseType.PostgreSQL => new PostgreSqlExtractor(request.ConnectionString),
                DatabaseType.SQLite => new SqliteExtractor(request.ConnectionString),
                _ => throw new NotImplementedException(),
            };

            _generator = new Generator(Cache.ApiKey).EnableChatHistory(50);

            FunctionCallingManager.RegisterFunction(nameof(_extractor.GetUserPermissionsAsync), "Get the current user's permissions in the database.");
            FunctionCallingManager.RegisterFunction(nameof(_extractor.GetDatabaseSchemaNamesAsync), "Search for the database schema names based on the given keyword", new GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling.Parameters
            {
                Properties = new
                {
                    keyword = new
                    {
                        type = "string",
                        description = "The keyword to filter the schema names. If empty, all schema names are returned."
                    }
                },
                Required = ["keyword"]
            });
            FunctionCallingManager.RegisterFunction(nameof(_extractor.GetSchemaInfoAsync), "Get schema information for a specified table within a given schema, including table structure, constraints, relationships, primary keys, and foreign keys", new GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling.Parameters
            {
                Properties = new
                {
                    schema = new
                    {
                        type = "string",
                        description = "The name of the schema containing the table. If not provided by the user, it should be the default schema of the database type (for example: `dbo` for SQL Server)"
                    },
                    table = new
                    {
                        type = "string",
                        description = "The name of the table for which to retrieve schema information. "
                    }
                },
                Required = ["table"]
            });
            FunctionCallingManager.RegisterFunction("RequestForActionPlan", "Request an action plan based on the current situation. This can be used when you got error or does not know what to do/how to do next", null);
            FunctionCallingManager.RegisterFunction("RequestForInternetSearch", "Request an internet search to gather more information or context about a specific topic or query.", new GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling.Parameters
            {
                Properties = new
                {
                    query = new
                    {
                        type = "string",
                        description = "Detailed description for the information to search on the internet, including the current context summarization, the information to search, and expected outcomes"
                    }
                },
                Required = ["query"]
            });
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var userInput = _userInputText.Trim();

            if (!string.IsNullOrEmpty(userInput))
            {
                SetMessage(userInput, true);

                await HandleUserInputAsync(userInput);
            }
        }

        private async Task HandleUserInputAsync(string userInput)
        {
            BadgeNotificationManager.Current.SetBadgeAsGlyph(BadgeNotificationGlyph.Activity);
            var instruction = await Extractor.ReadFile("Instructions/Global.md");
            instruction = instruction.Replace("{Database_Type}", _extractor.DatabaseType.ToString());
            var functionDeclarations = FunctionCallingManager.FunctionDeclarations;

            var apiRequest = new ApiRequestBuilder()
                .WithSystemInstruction(instruction)
                .WithPrompt(userInput)
                .DisableAllSafetySettings()
                .WithFunctionDeclarations(functionDeclarations, FunctionCallingMode.ANY)
                .Build();

            try
            {
                var modelResponse = await _generator.GenerateContentAsync(apiRequest, "gemini-2.5-flash-preview-05-20");

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
                                var output = $"**Error in function {function.Name}:**\n\n```plaintext\n{ex.Message}. {ex.InnerException?.Message}\n```\n\nPlease try with another approarch!";

                                functionResponses.Add(new FunctionResponse
                                {
                                    Name = function.Name,
                                    Response = new Response
                                    {
                                        Output = output,
                                    }
                                });
                                SetProgressContent(output, null, false, null);
                            }
                        }

                        var apiRequestWithFunctions = new ApiRequestBuilder()
                            .WithSystemInstruction(instruction)
                            .DisableAllSafetySettings()
                            .WithFunctionDeclarations(functionDeclarations, FunctionCallingMode.ANY)
                            .WithFunctionResponses(functionResponses)
                            .Build();

                        var modelResponseForFunction = await _generator.GenerateContentAsync(apiRequestWithFunctions, ModelVersion.Gemini_20_Flash_Lite);

                        SetProgressContent(modelResponseForFunction.Content, null, false, null);
                        await Task.Delay(1000);

                        functionCalls = (modelResponseForFunction.FunctionCalls == null || modelResponseForFunction.FunctionCalls.Count == 0) ? [] : modelResponseForFunction.FunctionCalls;
                    }
                    catch (Exception ex)
                    {
                        SetMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
                        functionCalls = [];
                    }
                }

                var apiRequestForConclusion = new ApiRequestBuilder()
                    .WithSystemInstruction(instruction)
                    .WithPrompt("Now summarize your actions, then provide any final insights or recommendations for the next steps.")
                    .DisableAllSafetySettings()
                    .Build();

                var modelResponseForConclusion = await _generator.GenerateContentAsync(apiRequestForConclusion, "gemini-2.0-flash");
                if (!string.IsNullOrEmpty(modelResponseForConclusion.Content))
                {
                    SetMessage(modelResponseForConclusion.Content, false);
                }
            }
            catch (Exception ex)
            {
                SetMessage($"Error: {ex.Message}", false);
            }
            finally
            {
                BadgeNotificationManager.Current.SetBadgeAsGlyph(BadgeNotificationGlyph.None);
            }
        }

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
                SqlCommand = sqlCommand ?? $"```sql\n{sqlCommand}\n```",
                ActionButtonVisibility = VisibilityHelper.SetVisible(isActionButtonVisible),
                Data = dataTable
            };

            ProgressContents.Add(progressContent);
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
                        SetProgressContent("Let me check your database.", null, false, null);
                        var dataTable = await _extractor.ExecuteQueryAsync(sqlQuery);
                        SetProgressContent("Let me take a look into this data", sqlQuery, true, dataTable);
                        return CreateResponse(FunctionCallingManager.ExecuteQueryAsyncFunction.Name, dataTable.ToMarkdown());
                    }
                case var name when name == FunctionCallingManager.ExecuteNonQueryAsyncFunction.Name:
                    {
                        var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                        SetProgressContent("Let me check your database.", sqlQuery, false, null);
                        await _extractor.ExecuteNonQueryAsync(sqlQuery);
                        SetProgressContent("Command executed successfully.", null, false, null);
                        return CreateResponse(FunctionCallingManager.ExecuteNonQueryAsyncFunction.Name, "Command executed successfully.");
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
                        SetProgressContent("Let me check your permissions.", null, false, null);
                        var permissions = await _extractor.GetUserPermissionsAsync();
                        var permissionsInMarkdown = string.Join(", ", permissions.Select(x => $"`{x}`"));
                        SetProgressContent($"I found these permissions: {permissionsInMarkdown}", null, false, null);
                        return CreateResponse(nameof(_extractor.GetUserPermissionsAsync), permissionsInMarkdown);
                    }
                case "RequestForActionPlan":
                    {
                        SetProgressContent("Let me think about the next steps.", null, false, null);
                        var actionPlanRequest = new ApiRequestBuilder()
                            .WithSystemInstruction(await Extractor.ReadFile("Instructions/Global.md"))
                            .DisableAllSafetySettings()
                            .WithPrompt(await Extractor.ReadFile("Instructions/Action Planning.md"))
                            .Build();
                        var actionPlanResponse = await _generator.GenerateContentAsync(actionPlanRequest, "gemini-2.5-flash-preview-05-20");
                        SetMessage(actionPlanResponse.Content, false);
                        return CreateResponse("RequestForActionPlan", actionPlanResponse.Content);
                    }
                case "RequestForInternetSearch":
                    {
                        var query = FunctionCallingHelper.GetParameterValue<string>(function, "query");
                        SetProgressContent($"Let me search the internet:\n\n {query}", null, false, null);
                        var searchRequest = new ApiRequestBuilder()
                            .WithSystemInstruction(await Extractor.ReadFile("Instructions/Global.md"))
                            .EnableGrounding()
                            .DisableAllSafetySettings()
                            .WithPrompt($"Now do an **in-depth internet research** and provide the result based on this description:\n\n{query}")
                            .Build();
                        var generator = new Generator(Cache.ApiKey);
                        var searchResponse = await generator.GenerateContentAsync(searchRequest, ModelVersion.Gemini_20_Flash_Lite);
                        SetMessage(searchResponse.Content, false);
                        return CreateResponse("RequestForInternetSearch", searchResponse.Content);
                    }
                default:
                    return null;
            }
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
                    Extractor.ExportCsv(dataTable, file.Path);
                    await DialogHelper.ShowSuccessAsync("CSV file has been exported to your selected location.");
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync(ex.Message);
            }
        }
    }
}
