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

        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;

            queryBox.KeyDown += QueryBox_KeyDown;
            queryBox.TextChanged += QueryBox_TextChanged;

            selectAllCheckbox.Click += SelectAllCheckbox_Click;

            sendButton.Click += SendButton_Click;
            showSqlButton.Click += ShowSqlButton_Click;
            exportButton.Click += ExportButton_Click;
            backButton.Click += BackButton_Click;
        }

        #region Events
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadTables();
                await LoadKeywords();
            }
            catch (Exception ex)
            {
                var result = await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
                if (result == ContentDialogResult.Primary)
                {
                    Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
                }
            }
        }

        private void QueryBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            sendButton.IsEnabled = true;

            if (e.Key == VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Tab)
            {
                var autoSuggestBox = sender as AutoSuggestBox;
                var suggestion = Cache.Data.Find(k => k.StartsWith(autoSuggestBox.Text, StringComparison.OrdinalIgnoreCase));
                if (suggestion != null)
                {
                    autoSuggestBox.Text = suggestion;
                    autoSuggestBox.Focus(FocusState.Programmatic);
                }
                e.Handled = true;
            }
        }
        private void QueryBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (Generator.CanBeGeminiApiKey(sender.Text) || string.IsNullOrEmpty(sender.Text))
                {
                    sender.ItemsSource = null;
                    sendButton.IsEnabled = false;
                    return;
                }

                var lastWord = StringEngineer.GetLastWord(sender.Text);

                sender.ItemsSource = Cache.Data
                       .Where(k => k.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                       .Take(10)
                       .OrderBy(k => k)
                       .Select(t => StringEngineer.ReplaceLastWord(sender.Text, lastWord, t));
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

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Analyzer.IsSqlSafe(queryBox.Text))
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, "You must not execute this dangerous command.", "Forbidden");
                return;
            }

            SetLoadingState(true);

            try
            {
                _resultDataTable = await Analyzer.DatabaseExtractor.GetData(queryBox.Text);
                SetButtonVisibility(true);
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
                await Cache.SetContent(_sqlQuery);

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
                    SuggestedFileName = "Exported Data"
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
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
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
            var sqlKeywords = await StringEngineer.GetLines(@$"Assets\SqlKeywords\{Analyzer.DatabaseExtractor.DatabaseType.ToString()}.txt");
            var commonKeywords = await StringEngineer.GetLines(@"Assets\PopularWords.txt");
            var tableNames = Analyzer.SelectedTables.Select(t => t.Name);
            var columnNames = Analyzer.SelectedTables.SelectMany(table => table.Columns.Select(column => column.Name));
            var removedKeywords = commonKeywords.Where(c => c.Length < 2);

            Cache.Data.AddRange(sqlKeywords);
            Cache.Data.AddRange(commonKeywords.Except(removedKeywords));
            Cache.Data.AddRange(tableNames);
            Cache.Data.AddRange(columnNames);

            Cache.Data = Cache.Data.AsParallel().Distinct().OrderBy(x => x).ToList();
        }

        private async Task ExecuteAnalyzedQuery(string query)
        {
            var sqlCommand = new SqlCommander();

            try
            {
                Analyzer.SelectedTables = Analyzer.DatabaseExtractor.Tables.Where(t => tablesListView.SelectedItems.Contains(t.Name)).ToList();
                sqlCommand = await Analyzer.GetSql(Generator.ApiKey, Analyzer.SelectedTables, query, Analyzer.DatabaseExtractor.DatabaseType);
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
                _resultDataTable = null;
                return;
            }

            if (!sqlCommand.IsSql)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, sqlCommand.Output, "Invalid SQL Command");
                _resultDataTable = null;
                return;
            }

            if (!Analyzer.IsSqlSafe(sqlCommand.Output))
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, "You must not execute this dangerous command.", "Forbidden");
                _resultDataTable = null;
                return;
            }

            _sqlQuery = sqlCommand.Output;

            try
            {
                _resultDataTable = await Analyzer.DatabaseExtractor.GetData(sqlCommand.Output);
                SetButtonVisibility(true);
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, $"SQL Command: {sqlCommand.Output}\n\n{ex.Message}");

                SetButtonVisibility(false);
                _resultDataTable = null;
            }
        }

        private void SetLoadingState(bool isLoading)
        {
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
