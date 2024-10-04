using DatabaseAnalyzer;
using DatabaseAnalyzer.Models;
using GenAI;
using Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using Cache = Helper.Cache;

namespace AskDB.App
{
    public sealed partial class MainPage : Page
    {
        private DataTable _resultDataTable = new();
        private string _sqlQuery;
        public static bool IsFirstEnter = true;

        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;

            queryBox.KeyDown += QueryBox_KeyDown;
            queryBox.KeyUp += QueryBox_KeyUp;
            queryBox.TextChanged += QueryBox_TextChanged;
            queryBox.SuggestionChosen += QueryBox_SuggestionChosen;

            selectAllCheckbox.Click += SelectAllCheckbox_Click;
            tablesListView.SelectionChanged += TablesListView_SelectionChanged;

            sendButton.Click += SendButton_Click;
            showSqlButton.Click += ShowSqlButton_Click;
            exportButton.Click += ExportButton_Click;
            showInsightButton.Click += ShowInsightButton_Click;
            backButton.Click += BackButton_Click;
        }


        #region Events
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsFirstEnter)
            {
                return;
            }

            try
            {
                queryBox.Text = string.Empty;
                SetLoadingState(true, "Analyzing your database structure");

                var prepareSampleTask = Analyzer.ExtractSampleData(15);

                var sqlQueryTask1 = Analyzer.GetSuggestedQueries(true);
                var sqlQueryTask2 = Analyzer.GetSuggestedQueries(true);
                var englishQueryTask1 = Analyzer.GetSuggestedQueries(false);
                var englishQueryTask2 = Analyzer.GetSuggestedQueries(false);

                await Task.WhenAll(prepareSampleTask, sqlQueryTask1, sqlQueryTask2, englishQueryTask1, englishQueryTask2);

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
                Cache.Data = [.. Cache.Data.AsParallel().OrderByDescending(k => k)];
                LoadTables();
                IsFirstEnter = false;
                SetLoadingState(false, string.Empty);
            }
        }

        private void QueryBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            SendButton_Click(sender, null);
        }
        private void QueryBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                if (StringTool.IsNull((sender as AutoSuggestBox).Text))
                {
                    (sender as AutoSuggestBox).ItemsSource = null;
                    sendButton.IsEnabled = false;
                    e.Handled = true;
                    return;
                }

                var autoSuggestBox = sender as AutoSuggestBox;
                var suggestion = Cache.Data.FirstOrDefault(k => k.StartsWith(autoSuggestBox.Text, StringComparison.OrdinalIgnoreCase));
                if (suggestion != null)
                {
                    autoSuggestBox.Text = suggestion;
                    autoSuggestBox.Focus(FocusState.Programmatic);
                    sendButton.IsEnabled = true;
                }

                e.Handled = true;
            }
        }
        private void QueryBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
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
                var query = sender.Text;

                if (!StringTool.IsNull(query))
                {
                    var source = Cache.Get(k => k.Contains(query, StringComparison.OrdinalIgnoreCase)
                        && !Generator.CanBeGeminiApiKey(k)
                        && !k.Contains(Generator.ApiKey, StringComparison.OrdinalIgnoreCase)
                        && !k.Contains(Analyzer.DbExtractor.ConnectionString, StringComparison.OrdinalIgnoreCase)
                        && !k.Contains(Extractor.GetEnumDescription(Analyzer.DbExtractor.DatabaseType), StringComparison.OrdinalIgnoreCase));

                    sender.ItemsSource = source;
                }

                sendButton.IsEnabled = !StringTool.IsNull(query);
            }
        }

        private void SelectAllCheckbox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;

            if (checkBox.IsChecked == true)
            {
                foreach (var item in tablesListView.Items)
                {
                    tablesListView.SelectedItems.Add(item);
                }
            }
            else
            {
                tablesListView.SelectedItems.Clear();
            }
        }
        private void TablesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectAllCheckbox.IsChecked = tablesListView.SelectedItems.Count == Analyzer.DbExtractor.Tables.Count;

            var selectedTableNames = tablesListView.SelectedItems.Cast<string>();
            var totalColumns = Analyzer.DbExtractor.Tables.Where(t => selectedTableNames.Contains(t.Name)).Sum(table => table.Columns.Count);
            if (totalColumns > Analyzer.MaxTotalColumns || tablesListView.SelectedItems.Count > Analyzer.MaxTotalTables)
            {
                sendButton.IsEnabled = queryBox.IsEnabled = false;
            }
            else
            {
                sendButton.IsEnabled = queryBox.IsEnabled = true;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SetLoadingState(true, "Executing your query");

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
                    SetLoadingState(false, string.Empty);
                    return;
                }
            }

            var selectedTableNames = tablesListView.SelectedItems.Cast<string>();
            Analyzer.SelectedTables = Analyzer.DbExtractor.Tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();

            try
            {
                _resultDataTable = await Analyzer.DbExtractor.Execute(queryBox.Text);
                SetButtonVisibility(true);
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

                SetLoadingState(false, string.Empty);
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
                    Extractor.ExportData(_resultDataTable, file.Path);
                }
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
            }
        }
        private async void ShowInsightButton_Click(object sender, RoutedEventArgs e)
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
                    SetLoadingState(true, "Analyzing your data");
                    try
                    {
                        var insight = await Analyzer.GetQuickInsight(query, _resultDataTable);
                        SetLoadingState(false, string.Empty);
                        await WinUiHelper.ShowDialog(RootGrid.XamlRoot, insight, "Quick Insight");
                    }
                    catch (Exception)
                    {
                        await WinUiHelper.ShowDialog(RootGrid.XamlRoot, $"Cannot analyze your data. Please try again.");
                    }
                    finally
                    {
                        SetLoadingState(false, string.Empty);
                    }

                }
            }
            else
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "There is not any data to analyze.");
            }
        }
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
            }
        }
        #endregion

        #region Helpers
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
                SetButtonVisibility(false);
                return;
            }

            if (!commander.IsSql)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, commander.Output, "Invalid Query");
                SetButtonVisibility(false);
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
                SetButtonVisibility(true);
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, $"SQL Command: {commander.Output}\n\n{ex.Message}");

                SetButtonVisibility(false);
            }
        }

        private void SetLoadingState(bool isLoading, string message)
        {
            backButton.IsEnabled = !isLoading;
            selectTableExpander.IsEnabled = sendButton.IsEnabled = !isLoading;
            LoadingOverlay.SetLoading(message, isLoading);
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            mainPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            selectTableExpander.IsExpanded = false;
        }
        private void SetButtonVisibility(bool isVisible)
        {
            exportButton.Visibility = showInsightButton.Visibility = showSqlButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            exportButton.IsEnabled = showInsightButton.IsEnabled = showSqlButton.IsEnabled = isVisible;
            if (!isVisible)
            {
                _resultDataTable = null;
            }
        }
        #endregion
    }
}
