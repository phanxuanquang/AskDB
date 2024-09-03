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
        private DataTable _resultDataTable = new DataTable();
        private string _sqlQuery;
        public static bool IsFirstEnter = true;

        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;

            queryBox.KeyDown += QueryBox_KeyDown;
            queryBox.KeyUp += QueryBox_KeyUp;
            queryBox.TextChanged += QueryBox_TextChanged;

            selectAllCheckbox.Click += SelectAllCheckbox_Click;
            tablesListView.SelectionChanged += TablesListView_SelectionChanged;

            sendButton.Click += SendButton_Click;
            showSqlButton.Click += ShowSqlButton_Click;
            exportButton.Click += ExportButton_Click;
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
                SetLoadingState(true);

                await Task.WhenAll(LoadSuggestedQueries(), LoadKeywords());

                Cache.Data = await Task.Run(() =>
                {
                    return Cache.Data.AsParallel().OrderByDescending(k => k).ToHashSet();
                });

                LoadTables();

                IsFirstEnter = false;
            }
            catch (Exception ex)
            {
                var result = await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
                if (result == ContentDialogResult.Primary)
                {
                    Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
                }
            }
            finally
            {
                SetLoadingState(false);
            }
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
                        && !k.Contains(Analyzer.DatabaseExtractor.ConnectionString, StringComparison.OrdinalIgnoreCase)
                        && !k.Contains(Extractor.GetEnumDescription(Analyzer.DatabaseExtractor.DatabaseType), StringComparison.OrdinalIgnoreCase));

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
            selectAllCheckbox.IsChecked = tablesListView.SelectedItems.Count == Analyzer.DatabaseExtractor.Tables.Count;
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

            SetLoadingState(true);

            var selectedTableNames = tablesListView.SelectedItems.Cast<string>();
            Analyzer.SelectedTables = Analyzer.DatabaseExtractor.Tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();

            try
            {
                _resultDataTable = await Analyzer.DatabaseExtractor.Execute(queryBox.Text);
                await Cache.Set(queryBox.Text);
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

                SetLoadingState(false);
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
                savePicker.FileTypeChoices.Add("CSV", new[] { ".csv" });

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
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
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
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
            }
        }
        #endregion

        #region Helpers
        private void LoadTables()
        {
            tablesListView.ItemsSource = Analyzer.DatabaseExtractor.Tables.Select(t => t.Name);

            foreach (var t in Analyzer.SelectedTables)
            {
                tablesListView.SelectedItems.Add(t.Name);
            }

            if (Analyzer.SelectedTables.Count == Analyzer.DatabaseExtractor.Tables.Count)
            {
                selectAllCheckbox.IsChecked = true;
            }
        }
        private async Task LoadKeywords()
        {
            var sqlKeywords = await StringTool.GetLines(@$"Assets\SqlKeywords\{Analyzer.DatabaseExtractor.DatabaseType.ToString()}.txt");
            var tableNames = Analyzer.SelectedTables.Select(t => t.Name);
            var columnNames = Analyzer.SelectedTables.AsParallel().SelectMany(table => table.Columns.Select(column => column.Name));

            await Cache.Set(sqlKeywords);
            await Cache.Set(tableNames);
            await Cache.Set(columnNames);
        }
        private async Task LoadSuggestedQueries()
        {
            var sqlQueriesTask = Analyzer.GetSuggestedQueries(Generator.ApiKey, Analyzer.DatabaseExtractor.DatabaseType, true);
            var englishQueriesTask = Analyzer.GetSuggestedQueries(Generator.ApiKey, Analyzer.DatabaseExtractor.DatabaseType, false);

            await Task.WhenAll(sqlQueriesTask, englishQueriesTask);

            var suggestedSqlQueries = await sqlQueriesTask;
            var suggestedEnglishQueries = await englishQueriesTask;

            await Task.WhenAll(Cache.Set(suggestedSqlQueries), Cache.Set(suggestedEnglishQueries));
        }

        private async Task ExecuteAnalyzedQuery(string query)
        {
            var commander = new SqlCommander();

            try
            {
                Analyzer.SelectedTables = Analyzer.DatabaseExtractor.Tables.Where(t => tablesListView.SelectedItems.Contains(t.Name)).ToList();
                commander = await Analyzer.GetSql(Generator.ApiKey, query, Analyzer.DatabaseExtractor.DatabaseType);
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
                _resultDataTable = null;
                return;
            }

            if (!commander.IsSql)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, commander.Output, "Invalid Query");
                _resultDataTable = null;
                return;
            }

            if (!Analyzer.IsSqlSafe(commander.Output))
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
                    _resultDataTable = null;
                    return;
                }
            }

            try
            {
                _resultDataTable = await Analyzer.DatabaseExtractor.Execute(commander.Output);
                await Cache.Set(commander.Output);
                _sqlQuery = commander.Output;
                SetButtonVisibility(true);
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, $"SQL Command: {commander.Output}\n\n{ex.Message}");

                SetButtonVisibility(false);
                _resultDataTable = null;
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            backButton.IsEnabled = !isLoading;
            selectTableExpander.IsEnabled = sendButton.IsEnabled = !isLoading;
            LoadingOverlay.SetLoading("Analyzing your database . . .", isLoading);
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            mainPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            selectTableExpander.IsExpanded = false;
        }
        private void SetButtonVisibility(bool isVisible)
        {
            exportButton.Visibility = showSqlButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            exportButton.IsEnabled = showSqlButton.IsEnabled = isVisible;
        }
        #endregion
    }
}
