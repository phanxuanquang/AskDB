using AskDB.App.Helpers;
using AskDB.App.View_Models;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseAnalyzer.Models;
using DatabaseInteractor.Helper;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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
            _generator = new Generator("AIzaSyAekYlgCpAGJXFFiekDqbQAB9SLIkyTIL4").EnableChatHistory(50);
            var connectionString = "Server=tcp:movie-rs.database.windows.net,1433;Initial Catalog=MovieRS;Persist Security Info=False;User ID=mimi;Password=ameowor1d#;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            var dataType = DatabaseType.SqlServer;

            _extractor = dataType switch
            {
                DatabaseType.MySQL => new MySqlExtractor(connectionString),
                DatabaseType.PostgreSQL => new PostgreSqlExtractor(connectionString),
                DatabaseType.SqlServer => new SqlServerExtractor(connectionString),
                DatabaseType.SQLite => new SqliteExtractor(connectionString),
                _ => throw new NotSupportedException($"Database type {dataType} is not supported."),
            };

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
                }
            });
            FunctionCallingManager.RegisterFunction(nameof(_extractor.GetSchemaInfoAsync), "Get schema information for a specified table within a given schema, including table structure, constraints, relationships, primary keys, and foreign keys", new GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling.Parameters
            {
                Properties = new
                {
                    schema = new
                    {
                        type = "string",
                        description = "The name of the schema containing the table. Cannot be null or empty."
                    },
                    table = new
                    {
                        type = "string",
                        description = "The name of the table for which to retrieve schema information. Cannot be null or empty."
                    }
                }
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
            var instruction = await Extractor.ReadFile("Instructions/Database Query.md");
            var functionDeclarations = FunctionCallingManager.FunctionDeclarations;

            var apiRequest = new ApiRequestBuilder()
                .WithSystemInstruction(instruction)
                .WithPrompt(userInput)
                .DisableAllSafetySettings()
                .WithFunctionDeclarations(functionDeclarations, FunctionCallingMode.AUTO)
                .Build();

            try
            {
                var modelResponse = await _generator.GenerateContentAsync(apiRequest, "gemini-2.0-flash");

                if (!string.IsNullOrEmpty(modelResponse.Content))
                {
                    SetProgressContent(modelResponse.Content, null, false, null);
                }

                var isTaskDone = modelResponse.FunctionCalls == null || modelResponse.FunctionCalls.Count == 0;

                while (!isTaskDone)
                {
                    try
                    {
                        var functionResponses = new List<FunctionResponse>();

                        foreach (var function in modelResponse.FunctionCalls)
                        {
                            try
                            {
                                if (function.Name == FunctionCallingManager.ExecuteQueryAsyncFunction.Name)
                                {
                                    var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                                    SetProgressContent("Let me check your database.", null, false, null);
                                    var dataTable = await _extractor.ExecuteQueryAsync(sqlQuery);
                                    functionResponses.Add(new FunctionResponse
                                    {
                                        Name = "ExecuteQueryAsync",
                                        Response = new Response
                                        {
                                            Output = dataTable.ToMarkdown(),
                                        }
                                    });
                                    SetProgressContent("Let me take a look into this data", sqlQuery, true, dataTable);

                                }
                                else if (function.Name == FunctionCallingManager.ExecuteNonQueryAsyncFunction.Name)
                                {
                                    var sqlQuery = FunctionCallingHelper.GetParameterValue<string>(function, "sqlQuery");
                                    SetProgressContent("Let me check your database.", sqlQuery, false, null);
                                    await _extractor.ExecuteNonQueryAsync(sqlQuery);
                                    functionResponses.Add(new FunctionResponse
                                    {
                                        Name = "ExecuteNonQueryAsync",
                                        Response = new Response
                                        {
                                            Output = "Command executed successfully.",
                                        }
                                    });
                                    SetProgressContent("Command executed successfully.", null, false, null);
                                }
                                else if (function.Name == nameof(_extractor.GetSchemaInfoAsync))
                                {
                                    var schema = FunctionCallingHelper.GetParameterValue<string>(function, "schema");
                                    var table = FunctionCallingHelper.GetParameterValue<string>(function, "table");
                                    SetProgressContent($"Let me check the schema information for the table`{schema}.{table}`", null, false, null);
                                    var schemaInfo = await _extractor.GetSchemaInfoAsync(schema, table);
                                    functionResponses.Add(new FunctionResponse
                                    {
                                        Name = nameof(_extractor.GetSchemaInfoAsync),
                                        Response = new Response
                                        {
                                            Output = schemaInfo.ToMarkdown(),
                                        }
                                    });
                                    SetProgressContent("Let me take a look into this data", null, false, schemaInfo);
                                }
                                else if (function.Name == nameof(_extractor.GetDatabaseSchemaNamesAsync))
                                {
                                    var keyword = FunctionCallingHelper.GetParameterValue<string>(function, "keyword");
                                    SetProgressContent($"Let me check the database schema names with the keyword `{keyword}`", null, false, null);
                                    var schemaNames = await _extractor.GetDatabaseSchemaNamesAsync(keyword);
                                    var schemaNamesInMarkdown = string.Join(", ", schemaNames.Select(x => $"`{x}`"));
                                    functionResponses.Add(new FunctionResponse
                                    {
                                        Name = nameof(_extractor.GetDatabaseSchemaNamesAsync),
                                        Response = new Response
                                        {
                                            Output = schemaNamesInMarkdown,
                                        }
                                    });
                                    SetProgressContent($"I found these tables: {schemaNamesInMarkdown}", null, false, null);
                                }
                                else if (function.Name == nameof(_extractor.GetUserPermissionsAsync))
                                {
                                    SetProgressContent("Let me check your permissions.", null, false, null);
                                    var permissions = await _extractor.GetUserPermissionsAsync();
                                    var permissionsInMarkdown = string.Join(", ", permissions.Select(x => $"`{x}`"));
                                    functionResponses.Add(new FunctionResponse
                                    {
                                        Name = nameof(_extractor.GetUserPermissionsAsync),
                                        Response = new Response
                                        {
                                            Output = permissionsInMarkdown,
                                        }
                                    });
                                    SetProgressContent($"I found these permissions: {permissionsInMarkdown}", null, false, null);
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
                            .WithFunctionDeclarations(functionDeclarations, FunctionCallingMode.AUTO)
                            .WithFunctionResponses(functionResponses)
                            .Build();

                        var modelResponseForFunction = await _generator.GenerateContentAsync(apiRequestWithFunctions, "gemini-2.0-flash");

                        isTaskDone = modelResponseForFunction.FunctionCalls == null || modelResponseForFunction.FunctionCalls.Count == 0;

                        if (isTaskDone)
                        {
                            SetMessage(modelResponseForFunction.Content, false);
                        }
                        else if (!string.IsNullOrEmpty(modelResponse.Content))
                        {
                            SetProgressContent(modelResponseForFunction.Content, null, false, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        SetMessage($"Error: {ex.Message}. {ex.InnerException?.Message}", false);
                        isTaskDone = true;
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessage($"Error: {ex.Message}", false);
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
    }
}
