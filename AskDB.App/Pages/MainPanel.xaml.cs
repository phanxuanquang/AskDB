using DatabaseAnalyzer;
using DatabaseAnalyzer.Models;
using Gemini.NET;
using Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPanel : Page
    {
        private DataTable _resultDataTable = new();
        private string _sqlQuery;

        public MainPanel()
        {
            this.InitializeComponent();
            Loaded += MainPanel_Loaded;

            queryBox.KeyDown += QueryBox_KeyDown;
            queryBox.KeyUp += QueryBox_KeyUp;
            queryBox.TextChanged += QueryBox_TextChanged;
            queryBox.SuggestionChosen += QueryBox_SuggestionChosen;

            sendButton.Click += SendButton_Click;
            showSqlButton.Click += ShowSqlButton_Click;
            exportButton.Click += ExportButton_Click;
            quickInsightButton.Click += QuickInsightButton_Click;
        }

        private async void QuickInsightButton_Click(object sender, RoutedEventArgs e)
        {
            if (_resultDataTable != null && _resultDataTable.Rows.Count != 0)
            {
                var query = queryBox.Text.Trim();
                var showInsight = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "Quick Insight",
                    Content = $"AskDB will analyze your data to provide some quick insights. This action also reveals your data to AskDB. The insight is based on the query: '{query}'\n\nAre you sure to continue?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No",
                    DefaultButton = ContentDialogButton.Primary
                };

                if (await showInsight.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        var insight = await Analyzer.GetQuickInsight(query, _resultDataTable);
                        await WinUiHelper.ShowDialog(RootGrid.XamlRoot, insight, "Quick Insight");
                    }
                    catch (Exception)
                    {
                        await WinUiHelper.ShowDialog(RootGrid.XamlRoot, $"Cannot analyze your data. Please try again.");
                    }
                }
            }
            else
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "There is not any data to analyze.");
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Analyzer.IsSqlSafe(queryBox.Text))
            {
                var warning = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "Warning",
                    Content = "This command can make changes to your database.\nAre you are to execute?",
                    PrimaryButtonText = "No",
                    SecondaryButtonText = "Yes",
                    DefaultButton = ContentDialogButton.Primary
                };

                if (await warning.ShowAsync() == ContentDialogResult.Primary)
                {
                    return;
                }
            }

            var selectedTableNames = tablesListView.SelectedItems.Cast<string>();
            Analyzer.SelectedTables = Analyzer.DbExtractor.Tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();

            try
            {
                _resultDataTable = await Analyzer.DbExtractor.Execute(queryBox.Text);
                showSqlButton.Visibility = Visibility.Collapsed;
            }
            catch
            {
                await ExecuteAnalyzedQuery(queryBox.Text);
            }
            finally
            {
                resultTable.Columns.Clear();

                if (_resultDataTable != null)
                {
                    for (int i = 0; i < _resultDataTable.Columns.Count; i++)
                    {
                        resultTable.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn()
                        {
                            Header = _resultDataTable.Columns[i].ColumnName,
                            Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                        });
                    }

                    var collectionObjects = new System.Collections.ObjectModel.ObservableCollection<object>(
                        _resultDataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray)
                    );

                    resultTable.ItemsSource = collectionObjects;
                }
            }
        }
        private async void ShowSqlButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "SQL Query",
                PrimaryButtonText = "Copy",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                Content = _sqlQuery
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Cache.Set(_sqlQuery);

                var dataPackage = new DataPackage();
                dataPackage.SetText(_sqlQuery);
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }
        }
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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
                    Extractor.ExportCsv(_resultDataTable, file.Path);
                }
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
            }
        }

        private void QueryBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
            SendButton_Click(sender, null);
        }
        private void QueryBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                var autoSuggestBox = sender as AutoSuggestBox;
                var keyword = autoSuggestBox.Text.Trim();

                if (StringTool.IsNull(keyword))
                {
                    (sender as AutoSuggestBox).ItemsSource = null;
                    sendButton.IsEnabled = false;
                    e.Handled = true;
                    return;
                }

                var suggestion = Cache.Get(k => !Validator.CanBeValidApiKey(k)
                        && !k.Contains(Cache.ApiKey, StringComparison.OrdinalIgnoreCase)
                        && !k.Contains(Analyzer.DbExtractor.ConnectionString, StringComparison.OrdinalIgnoreCase)
                        && !k.Contains(Extractor.GetEnumDescription(Analyzer.DbExtractor.DatabaseType), StringComparison.OrdinalIgnoreCase), keyword)
                    .FirstOrDefault();

                if (suggestion != null)
                {
                    autoSuggestBox.Text = suggestion;
                    autoSuggestBox.Focus(FocusState.Programmatic);
                    sendButton.IsEnabled = true;
                }

                e.Handled = true;
            }
        }
        private void QueryBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (StringTool.IsNull((sender as AutoSuggestBox).Text))
                {
                    (sender as AutoSuggestBox).ItemsSource = null;
                    sendButton.IsEnabled = false;
                    return;
                }

                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }
        private void QueryBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text.Trim();

                if (!StringTool.IsNull(query))
                {
                    var source = Cache.Get(k => !Validator.CanBeValidApiKey(k)
                        && !k.Contains(Cache.ApiKey, StringComparison.OrdinalIgnoreCase)
                        && !k.Contains(Analyzer.DbExtractor.ConnectionString, StringComparison.OrdinalIgnoreCase)
                        && !k.Contains(Extractor.GetEnumDescription(Analyzer.DbExtractor.DatabaseType), StringComparison.OrdinalIgnoreCase), query);

                    sender.ItemsSource = source;
                }

                sendButton.IsEnabled = !StringTool.IsNull(query);
            }
        }

        private async void MainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                queryBox.Text = string.Empty;

                await Analyzer.ExtractSampleData(15);

                var sqlQueryTask1 = Analyzer.GetSuggestedQueries(true);
                var sqlQueryTask2 = Analyzer.GetSuggestedQueries(true);
                var englishQueryTask1 = Analyzer.GetSuggestedQueries(false);
                var englishQueryTask2 = Analyzer.GetSuggestedQueries(false);

                await Task.WhenAll(sqlQueryTask1, sqlQueryTask2, englishQueryTask1, englishQueryTask2);

                await Cache.Set(sqlQueryTask1.Result);
                await Cache.Set(sqlQueryTask2.Result);
                await Cache.Set(englishQueryTask1.Result);
                await Cache.Set(englishQueryTask2.Result);
            }
            catch
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Cannot load suggested queries. Try starting again in the previous page if you need the suggested queries.", "Warning");
            }
            finally
            {
                LoadTables();
            }
        }

        private void LoadTables()
        {
            tablesListView.ItemsSource = Analyzer.DbExtractor.Tables.Select(t => t.Name);

            foreach (var t in Analyzer.SelectedTables)
            {
                tablesListView.SelectedItems.Add(t.Name);
            }

            if (Analyzer.SelectedTables.Count == Analyzer.DbExtractor.Tables.Count)
            {
                selectAllCheckbox.IsChecked = true;
            }
        }
        private async Task ExecuteAnalyzedQuery(string query)
        {
            var commander = new SqlCommander();

            try
            {
                Analyzer.SelectedTables = Analyzer.DbExtractor.Tables.Where(t => tablesListView.SelectedItems.Contains(t.Name)).ToList();
                commander = await Analyzer.GetSql(query);
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
                return;
            }

            if (!commander.IsSql)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, commander.Output, "Invalid Query");
                return;
            }

            if (!Analyzer.IsSqlSafe(commander.Output))
            {
                var warning = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "Warning",
                    Content = "This command can make changes to your database.\nAre you sure to execute?",
                    PrimaryButtonText = "No",
                    SecondaryButtonText = "Yes",
                    DefaultButton = ContentDialogButton.Primary
                };

                if (await warning.ShowAsync() == ContentDialogResult.Primary)
                {
                    _resultDataTable = null;
                    return;
                }
            }

            try
            {
                _resultDataTable = await Analyzer.DbExtractor.Execute(commander.Output);
                _sqlQuery = commander.Output;
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, $"SQL Command: {commander.Output}\n\n{ex.Message}");
            }
        }
    }
}
