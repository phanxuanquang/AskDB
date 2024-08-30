using DatabaseAnalyzer;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using Cache = Helper.Cache;
using Table = DatabaseAnalyzer.Models.Table;

namespace AskDB.App
{
    public sealed partial class MainPage : Page
    {
        private DataTable DataTable = new DataTable();
        private List<Table> Tables = new List<Table>();
        private string SqlQuery;
        private List<string> Keywords = new List<string>();

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
                await LoadTables();
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
                var suggestions = Keywords.Where(k => k.ToUpper().StartsWith(lastWord.ToUpper())).Take(10).OrderBy(k => k).Select(t => StringEngineer.ReplaceLastOccurrence(sender.Text, lastWord, t)).ToList();
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
                    var suggestion = Keywords.FirstOrDefault(k => k.ToUpper().StartsWith(lastWord.ToUpper()));

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
                IDatabaseExtractor extractor;

                switch (Analyzer.DatabaseType)
                {
                    case DatabaseType.PostgreSQL:
                        extractor = new PostgreSqlExtractor();
                        break;
                    case DatabaseType.MySQL:
                        extractor = new MySqlExtractor();
                        break;
                    case DatabaseType.SQLite:
                        extractor = new SqliteExtractor();
                        break;
                    default:
                        extractor = new SqlServerExtractor();
                        break;
                }

                var isQuerySql = await TryExecuteDirectSql(extractor);

                if (!isQuerySql)
                {
                    await ExecuteAnalyzedQuery(extractor);
                }

                UpdateOutputGrid();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(ex.Message);
            }
            finally
            {
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
        private async Task LoadTables()
        {
            Tables = await Analyzer.GetTables(Analyzer.DatabaseType, Analyzer.ConnectionString);

            tablesListView.ItemsSource = Tables.Select(t => t.Name);

            foreach (var t in Analyzer.Tables)
            {
                tablesListView.SelectedItems.Add(t.Name);
            }
        }

        private async Task LoadKeywords()
        {
            var sqlKeywords = await StringEngineer.GetWords(@$"Assets\SqlKeywords\{Analyzer.DatabaseType.ToString()}.txt");
            var commonKeywords = await StringEngineer.GetWords(@"Assets\PopularWords.txt");
            var tableNames = Analyzer.Tables.Select(t => t.Name);
            var columnNames = Analyzer.Tables.SelectMany(table => table.Columns.Select(column => column.Name));
            var clipboardContent = await Cache.GetContent();

            Keywords.AddRange(sqlKeywords);
            Keywords.AddRange(commonKeywords);
            Keywords.AddRange(tableNames);
            Keywords.AddRange(columnNames);
            Keywords.AddRange(clipboardContent);

            Keywords = Keywords.Distinct().OrderBy(x => x).ToList();
        }

        private async Task<bool> TryExecuteDirectSql(IDatabaseExtractor extractor)
        {
            try
            {
                DataTable = await extractor.GetData(Analyzer.ConnectionString, queryBox.Text);
                exportButton.Visibility = Visibility.Visible;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ExecuteAnalyzedQuery(IDatabaseExtractor extractor)
        {
            Analyzer.Tables = Tables.Where(t => tablesListView.SelectedItems.Contains(t.Name)).ToList();
            var sqlCommand = await Analyzer.GetSql(Analyzer.ApiKey, Analyzer.Tables, queryBox.Text, Analyzer.DatabaseType);

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

                return;
            }

            SqlQuery = sqlCommand.Output;

            try
            {
                DataTable = await extractor.GetData(Analyzer.ConnectionString, sqlCommand.Output);
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
