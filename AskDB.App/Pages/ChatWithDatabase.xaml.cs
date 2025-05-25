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
                        description = "The name of the schema containing the table, can be empty."
                    },
                    table = new
                    {
                        type = "string",
                        description = "The name of the table for which to retrieve schema information. Cannot be null or empty."
                    }
                }
            });
            FunctionCallingManager.RegisterFunction("RequestForActionPlan", "Request an action plan based on the current situation. This can be used when you got error or does not know what to do/how to do next", null);
            FunctionCallingManager.RegisterFunction("RequestForInternetSearch", "Request an internet search to gather more information or context about a specific topic or query.", new GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling.Parameters
            {
                Properties = new
                {
                    query = new
                    {
                        type = "string",
                        description = "Detailed description for the information to search on the internet, including the keywords and expected outcomes"
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

                if(functionCalls.Count == 0)
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
                                    var schemaInfo = await _extractor.GetSchemaInfoAsync(table, schema);
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
                                    SetProgressContent($"I found these schemas: {schemaNamesInMarkdown}", null, false, null);
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
                                else if(function.Name == "RequestForActionPlan")
                                {
                                    SetProgressContent("Let me think about the next steps.", null, false, null);
                                    var actionPlanRequest = new ApiRequestBuilder()
                                        .WithSystemInstruction(instruction)
                                        .DisableAllSafetySettings()
                                        .WithPrompt(@"We have encountered a deviation from the expected outcome or a point of uncertainty. I require you to execute the following structured problem-solving and action planning protocol:

1.  **Problem Statement Definition:**
    *   Based on the current context, my previous request, and any errors or unexpected results observed, explicitly define the precise problem or knowledge gap you are currently facing.

2.  **Root Cause Analysis (Leveraging Reasoning):**
    *   Conduct a thorough analysis to identify the most probable root cause(s) of the defined problem. Consider factors such as:
        *   Misinterpretation of my previous instructions.
        *   Incorrect assumptions made.
        *   Data inconsistencies or limitations.
        *   Tool usage errors or limitations.
        *   Logical flaws in the previous plan.
    *   State your deduced root cause(s).

3.  **Goal Reaffirmation:**
    *   Briefly restate my original, overarching goal for this interaction (e.g., ""[briefly restate your initial objective, like 'retrieve sales data for Q1 for X product']""). This is to ensure your plan aligns with it.

4.  **Proposed Action Plan Formulation:**
    *   Develop a new, step-by-step action plan designed to:
        a.  Directly address the identified root cause(s).
        b.  Progress towards achieving the reaffirmed goal.
    *   Each step in your plan must be concrete, actionable, and logical.
    *   For each step, specify:
        *   The action to be taken.
        *   (If applicable) The specific tool you intend to use.
        *   (If applicable) The information you expect to gain or the state you expect to achieve.

5.  **Rationale for the Plan:**
    *   For the overall action plan, provide a concise justification explaining *why* you believe this sequence of steps is the most effective and logical path forward, given your root cause analysis and the reaffirmed goal.

Please present your response clearly, addressing each of the five points above in sequence.")
                                        .Build();

                                    var actionPlanResponse = await _generator.GenerateContentAsync(actionPlanRequest, "gemini-2.5-flash-preview-05-20");
                                    functionResponses.Add(new FunctionResponse
                                    {
                                        Name = "RequestForActionPlan",
                                        Response = new Response
                                        {
                                            Output = actionPlanResponse.Content,
                                        }
                                    });
                                    SetMessage(actionPlanResponse.Content, false);
                                }
                                else if (function.Name == "RequestForInternetSearch")
                                {
                                    var query = FunctionCallingHelper.GetParameterValue<string>(function, "query");
                                    SetProgressContent($"Let me search the internet:\n\n {query}", null, false, null);
                                    var searchRequest = new ApiRequestBuilder()
                                        .WithSystemInstruction(instruction)
                                        .EnableGrounding()
                                        .DisableAllSafetySettings()
                                        .WithPrompt(query)
                                        .Build();

                                    var searchResponse = await _generator.GenerateContentAsync(searchRequest, ModelVersion.Gemini_20_Flash_Lite);
                                    functionResponses.Add(new FunctionResponse
                                    {
                                        Name = "RequestForInternetSearch",
                                        Response = new Response
                                        {
                                            Output = searchResponse.Content,
                                        }
                                    });
                                    SetMessage(searchResponse.Content, false);
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

                        functionCalls = (modelResponseForFunction.FunctionCalls == null || modelResponseForFunction.FunctionCalls.Count == 0) ? [] : modelResponseForFunction.FunctionCalls;

                        if(functionCalls.Count == 0)
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
