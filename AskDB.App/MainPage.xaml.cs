using DatabaseAnalyzer;
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
        private DataTable DataTable = new DataTable();
        private string SqlQuery;

        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;

            queryBox.TextChanged += QueryBox_TextChanged;
            queryBox.KeyDown += QueryBox_KeyDown;

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
                var result = await ShowErrorDialog(ex.Message);
                if (result == ContentDialogResult.Primary)
                {
                    Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
                }
            }
        }

        private void QueryBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrEmpty(sender.Text))
                {
                    return;
                }

                var lastWord = StringEngineer.GetLastWord(sender.Text);
                var suggestions = Analyzer.Keywords.Where(k => k.ToUpper().StartsWith(lastWord.ToUpper())).Take(10).OrderBy(k => k).Select(t => StringEngineer.ReplaceLastOccurrence(sender.Text, lastWord, t)).ToList();
                sender.ItemsSource = suggestions;
            }
        }
        private void QueryBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty((sender as AutoSuggestBox).Text))
            {
                return;
            }

            if (e.Key == VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Tab)
            {
                var autoSuggestBox = sender as AutoSuggestBox;
                var query = autoSuggestBox.Text;
                var lastWord = StringEngineer.GetLastWord(query);

                if (!string.IsNullOrEmpty(lastWord))
                {
                    var suggestion = Analyzer.Keywords.FirstOrDefault(k => k.ToUpper().StartsWith(lastWord.ToUpper()));

                    if (suggestion != null)
                    {
                        autoSuggestBox.Text = StringEngineer.ReplaceLastOccurrence(query, lastWord, suggestion);

                        autoSuggestBox.Focus(FocusState.Programmatic);

                        e.Handled = true;
                    }

                    e.Handled = true;
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(queryBox.Text))
            {
                return;
            }

            SetLoadingState(true);

            try
            {
                var isQuerySql = await TryExecuteDirectSql();

                if (!isQuerySql)
                {
                    await ExecuteAnalyzedQuery();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(ex.Message);
            }
            finally
            {
                UpdateOutputGrid();
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
                Content = SqlQuery
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Cache.SetContent(SqlQuery);

                var dataPackage = new DataPackage();
                dataPackage.SetText(SqlQuery);
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
                    Extractor.ExportData(DataTable, file.Path);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(ex.Message);
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
        }

        private async Task LoadKeywords()
        {
            var sqlKeywords = await StringEngineer.GetWords(@$"Assets\SqlKeywords\{Analyzer.DatabaseExtractor.DatabaseType.ToString()}.txt");
            var commonKeywords = await StringEngineer.GetWords(@"Assets\PopularWords.txt");
            var tableNames = Analyzer.SelectedTables.Select(t => t.Name);
            var columnNames = Analyzer.SelectedTables.SelectMany(table => table.Columns.Select(column => column.Name));

            Analyzer.Keywords.AddRange(sqlKeywords);
            Analyzer.Keywords.AddRange(commonKeywords);
            Analyzer.Keywords.AddRange(tableNames);
            Analyzer.Keywords.AddRange(columnNames);

            Analyzer.Keywords = Analyzer.Keywords.Distinct().OrderBy(x => x).ToList();
        }

        private async Task<bool> TryExecuteDirectSql()
        {
            try
            {
                DataTable = await Analyzer.DatabaseExtractor.GetData(queryBox.Text);
                exportButton.Visibility = Visibility.Visible;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ExecuteAnalyzedQuery()
        {
            Analyzer.SelectedTables = Analyzer.DatabaseExtractor.Tables.Where(t => tablesListView.SelectedItems.Contains(t.Name)).ToList();
            var sqlCommand = await Analyzer.GetSql(Analyzer.ApiKey, Analyzer.SelectedTables, queryBox.Text, Analyzer.DatabaseExtractor.DatabaseType);

            if (!sqlCommand.IsSql)
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "Not an SQL command",
                    PrimaryButtonText = "OK",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = sqlCommand.Output
                };

                await dialog.ShowAsync();
                DataTable = null;
                return;
            }

            SqlQuery = sqlCommand.Output;

            try
            {
                DataTable = await Analyzer.DatabaseExtractor.GetData(sqlCommand.Output);
                SetButtonVisibility(true);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "SQL Error",
                    PrimaryButtonText = "OK",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = $"SQL Command: {sqlCommand.Output}\n\n{ex.Message}"
                };

                await dialog.ShowAsync();

                SetButtonVisibility(false);
                DataTable = null;
            }
        }

        private async Task<ContentDialogResult> ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Error",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                Content = message
            };

            return await dialog.ShowAsync();
        }

        private void UpdateOutputGrid()
        {
            outputGridView.Columns.Clear();

            if (DataTable != null)
            {
                for (int i = 0; i < DataTable.Columns.Count; i++)
                {
                    outputGridView.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn()
                    {
                        Header = DataTable.Columns[i].ColumnName,
                        Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                    });
                }

                var collectionObjects = new System.Collections.ObjectModel.ObservableCollection<object>(
                    DataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray)
                );

                outputGridView.ItemsSource = collectionObjects;
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
