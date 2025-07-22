using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.Commons.Extensions;
using AskDB.SemanticKernel.Extensions;
using AskDB.SemanticKernel.Plugins;
using AskDB.SemanticKernel.Services;
using CommunityToolkit.WinUI.UI.Controls;
using DatabaseInteractor.Factories;
using DatabaseInteractor.Helpers;
using DatabaseInteractor.Services;
using GeminiDotNET.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using WinRT.Interop;

namespace AskDB.App.Pages
{
    public sealed partial class ChatWithDatabase : Page, INotifyPropertyChanged, IFunctionInvocationFilter
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly ObservableCollection<ChatMessage> Messages = [];
        private readonly ObservableCollection<AgentSuggestion> AgentSuggestions = [];
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

        private DatabaseInteractionService _databaseInteractor;
        private AgentChatCompletionService _chatCompletionService;

        private string _globalInstruction;
        private string _actionPlanInstruction;
        private bool _isLoading = false;
        private bool _isImeActive = true;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ChatWithDatabase()
        {
            this.InitializeComponent();
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        }

        #region Event Handlers
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not DatabaseConnectionInfo request)
            {
                return;
            }

            SetLoading(true);

            try
            {
                _databaseInteractor = request.DatabaseType.CreateInteractionService(request.ConnectionString);

                var globalTask = OnlineContentHelper.GetSytemInstructionContentAsync("Global", _databaseInteractor.DatabaseType, "English");
                var actionPlanTask = OnlineContentHelper.GetSytemInstructionContentAsync("Action Plan", _databaseInteractor.DatabaseType, "English");
                var initQueryTemplatesTask = _databaseInteractor.InitQueryTemplatesAsync();

                await Task.WhenAll(globalTask, actionPlanTask, initQueryTemplatesTask);

                _globalInstruction = await globalTask;
                _actionPlanInstruction = await actionPlanTask;

                _globalInstruction = _globalInstruction.Replace("{Language}", "English");

                _chatCompletionService = new AgentChatCompletionService(Cache.KernelFactory
                        .WithPlugin(new DatabaseInteractionPlugin(_databaseInteractor))
                        .WithFunctionInvocationFilter(this))
                    .WithSystemInstruction(_globalInstruction);

                await LoadTableNamesAsync();
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
        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (Messages.Count == 0)
            {
                await DialogHelper.ShowErrorAsync("You have not sent any message to AskDB");
            }

            var result = await DialogHelper.ShowDialogWithOptions("Reset the conversation", "This action will clear our conversation. Are you sure to proceed?", "Yes");
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            SetLoading(true);

            try
            {
                await ResetConversationAsync();
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                await ShowInforBarAsync($"Error while resetting the conversation: {ex.Message}", false);
            }

            SetLoading(false);
        }

        private void QueryBox_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            _isImeActive = false;
        }
        private void QueryBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            var textbox = sender as TextBox;

            if (textbox == null)
            {
                return;
            }

            if (e.Key == VirtualKey.Enter
                && !InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)
                && !string.IsNullOrWhiteSpace(textbox.Text)
                && !_isImeActive)
            {
                var cursorPosition = textbox.SelectionStart;
                var text = textbox.Text;
                if (cursorPosition > 0 && (text[cursorPosition - 1] == '\n' || text[cursorPosition - 1] == '\r'))
                {
                    text = text.Remove(cursorPosition - 1, 1);
                    textbox.Text = text;
                }

                textbox.SelectionStart = cursorPosition - 1;

                var currentPlaceholder = textbox.PlaceholderText;

                textbox.PlaceholderText = "Please wait for the response to complete before entering a new message";
                SendButton_Click(sender, e);
                textbox.PlaceholderText = currentPlaceholder;
            }
            else
            {
                _isImeActive = true;
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

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            switch (context.Function.PluginName)
            {
                case nameof(DatabaseInteractionPlugin):
                    await CallDatabaseInteractionFunctionAsync(context);
                    break;
                default:
                    // For other plugins, just continue to the next step
                    break;
            }

            await next(context);
        }
        #endregion

        #region Message and Progress Content Bindings
        private void SetLoading(bool isLoading)
        {
            IsLoading = isLoading;
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
        private void SetAgentMessage(string? message, DataTable? dataTable = null, long? queryResultId = null)
        {
            var isDataTableEmpty = dataTable == null || dataTable.Rows.Count == 0;

            var chatMessage = new ChatMessage
            {
                Message = message,
                IsFromUser = false,
                IsFromAgent = true,
                Data = isDataTableEmpty ? null : dataTable,
                QueryResultId = queryResultId
            };

            _dispatcherQueue.TryEnqueue(() =>
            {
                Messages.Add(chatMessage);
            });
        }
        #endregion

        private async Task HandleUserInputAsync(string userInput)
        {
            try
            {
                SetLoading(true);
                SetUserMessage(userInput);

                AgentSuggestionsItemView.Visibility = VisibilityHelper.SetVisible(false);
                AgentSuggestions.Clear();

                var result = await _chatCompletionService.SendMessageAsync(userInput);

                SetAgentMessage(result.Content);

                await LoadAgentSuggestionsAsync("Based on the current context, please provide up to 5 **short** and **concise** suggestions as the next step in my viewpoint, for me to use as the quick reply in order to continue the task.");
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                SetAgentMessage($"**Error:**\n\n```console\n{ex.Message}. {ex.InnerException?.Message}\n```\n\n.\n\n*The reason detail has been copied to your clipboard.*");
            }
            finally
            {
                SetLoading(false);
            }
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
                var tableNames = await _databaseInteractor.SearchTablesByNameAsync(null);
                _databaseInteractor.CachedAllTableNames.UnionWith(tableNames);

                return tableNames;
            }

            return [];
        }

        public class UserResponseSuggestion
        {
            public List<string> UserResponseSuggestions { get; set; }
        }

        private async Task LoadAgentSuggestionsAsync(string prompt)
        {
            AgentSuggestions.Clear();

            try
            {
                var response = await _chatCompletionService.SendMessageAsync<UserResponseSuggestion>(prompt);

                foreach (var item in response.UserResponseSuggestions)
                {
                    AgentSuggestions.Add(new AgentSuggestion
                    {
                        UserResponseSuggestion = item
                    });
                }

                AgentSuggestionsItemView.Visibility = VisibilityHelper.SetVisible(true);
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                await ShowInforBarAsync($"Cannot load suggestions: {ex.Message}", false);
            }
        }

        private async Task ResetConversationAsync()
        {
            Messages.Clear();
            AgentSuggestions.Clear();

            var tableNames = await LoadTableNamesAsync();

            if (tableNames.Count > 0)
            {
                await LoadAgentSuggestionsAsync($@"Based on the list of table names provided below, suggest me with 5 concise and interesting ideas from my viewpoint that I might ask to start the conversation.
The suggested ideas should be phrased as questions or commands in natural language, assuming I am not technical but want to understand and explore the data for analysis purpose or predictional purpose.
Focus on common exploratory intents such as: viewing recent records, counting items, finding top or recent entries, understanding relationships, checking for missing/empty data, searching for specific information, or exploring the table structure, etc.
Avoid SQL or technical jargon in the suggestions. Each suggestion should be unique, short, concise, specific, user-friendly, and **MUST NOT** be relevant to sensitive, security-related, or credential-related tables, and do not suggest actions that require elevated permissions or could lead to data loss or sensitive information exposure.

This is the list of table names in the database: {string.Join(", ", tableNames.Select(x => $"`{x}`"))}");
            }

            var response = await _chatCompletionService.SendMessageAsync("In order to start the conversation, please introduce to me about yourself, such as who you are, what you can do, what you can help me, or anything else that you think it may be relavant to my database and be useful to me; and some good practices for me to help you to do the task effectively. Take me as your friend or your teammate, avoid to use formal-like tone while talking to me; just use a natural, friendly tone with daily-life word when talking to me, like you are talking with your friends in the real life.");

            SetAgentMessage(response.ToString());
        }

        public async Task CallDatabaseInteractionFunctionAsync(FunctionInvocationContext context)
        {
            switch (context.Function.Name)
            {
                case nameof(DatabaseInteractionPlugin.SearchTablesByName):
                    {
                        var keyword = context.GetFunctionArgument<string>("keyword");
                        var tableNames = await _databaseInteractor.SearchTablesByNameAsync(keyword);
                        if (tableNames.Count > 0)
                        {
                            SetAgentMessage($"Found {tableNames.Count} tables matching `{keyword}`: {string.Join(", ", tableNames.Select(name => $"`{name}`"))}");
                        }
                        else
                        {
                            SetAgentMessage($"No tables found matching `{keyword}`.");
                        }
                        break;
                    }
                case nameof(DatabaseInteractionPlugin.GetUserPermissions):
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
                        break;
                    }
                case nameof(DatabaseInteractionPlugin.ExecuteQuery):
                    {
                        var sqlQuery = context.GetFunctionArgument<string>("sqlQuery");
                        if (string.IsNullOrEmpty(sqlQuery))
                        {
                            SetAgentMessage("No SQL query provided.");
                            break;
                        }
                        try
                        {
                            var dataTable = await _databaseInteractor.ExecuteQueryAsync(sqlQuery);
                            if (dataTable != null && dataTable.Rows.Count > 0)
                            {
                                SetAgentMessage($"Let me execute this query to check the data:\n\n```sql\n{sqlQuery}\n```", dataTable);
                            }
                            else
                            {
                                SetAgentMessage("No data found for the query.");
                            }
                        }
                        catch (Exception ex)
                        {
                            SetAgentMessage($"Error while executing your query.\n\n```sql\n{sqlQuery}\n```\n\n```console\n{ex.Message}\n```");
                        }
                        break;
                    }
                case nameof(DatabaseInteractionPlugin.ExecuteNonQuery):
                    {
                        var sqlCommand = context.GetFunctionArgument<string>("sqlCommand");
                        SetAgentMessage($"Let me execute the command:\n\n```sql\n{sqlCommand}\n```");
                        break;
                    }
                case nameof(DatabaseInteractionPlugin.GetTableStructureDetail):
                    {
                        var tableName = context.GetFunctionArgument<string>("table") ?? string.Empty;
                        var schemaName = context.GetFunctionArgument<string>("schema");
                        var tableStructure = await _databaseInteractor.GetTableStructureDetailAsync(schemaName, tableName);
                        if (tableStructure.Rows.Count > 0)
                        {
                            SetAgentMessage(null, tableStructure);
                        }
                        else
                        {
                            SetAgentMessage($"No structure found for table '{tableName}'.");
                        }
                        break;
                    }
                default: break;
            }
        }
    }
}
