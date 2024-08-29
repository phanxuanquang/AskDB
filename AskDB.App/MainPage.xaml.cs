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
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Table = DatabaseAnalyzer.Models.Table;

namespace AskDB.App
{
    public sealed partial class MainPage : Page
    {
        private DataTable DataTable = new DataTable();
        private List<Table> Tables = new List<Table>();
        private string SqlQuery;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            queryBox.KeyDown += QueryBox_KeyDown;
            showSqlButton.Click += ShowSqlButton_Click;
            exportButton.Click += ExportButton_Click;
            backButton.Click += BackButton_Click;
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = await PickSaveFile();
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

        private async Task<StorageFile> PickSaveFile()
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = "Exported Data"
            };
            savePicker.FileTypeChoices.Add("CSV", new[] { ".csv" });

            nint windowHandle = WindowNative.GetWindowHandle(App.Window);
            InitializeWithWindow.Initialize(savePicker, windowHandle);

            return await savePicker.PickSaveFileAsync();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
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
                CopyToClipboard(SqlQuery);
            }
        }

        private void CopyToClipboard(string text)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
        }

        private void QueryBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadTables();
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

        private async Task LoadTables()
        {
            Tables = await Analyzer.GetTables(Analyzer.DatabaseType, Analyzer.ConnectionString);
            tablesListView.ItemsSource = Tables.Select(t => t.Name).ToList();

            foreach (var t in Analyzer.Tables)
            {
                tablesListView.SelectedItems.Add(t.Name);
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SetLoadingState(true);

            try
            {
                await ProcessQuery();
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

        private async Task ProcessQuery()
        {
            var extractor = GetDatabaseExtractor();
            var isQuerySql = await TryExecuteDirectSql(extractor);

            if (!isQuerySql)
            {
                await ExecuteAnalyzedQuery(extractor);
            }

            UpdateOutputGrid();
        }

        private IDatabaseExtractor GetDatabaseExtractor()
        {
            switch (Analyzer.DatabaseType)
            {
                case DatabaseType.PostgreSQL:
                    return new PostgreSqlExtractor();
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    return new MySqlExtractor();
                case DatabaseType.SQLite:
                    return new SqliteExtractor();
                default:
                    return new SqlServerExtractor();
            }
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
                await ShowNotSqlDialog(sqlCommand.Output);
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
                await ShowSqlErrorDialog(sqlCommand.Output, ex.Message);
                SetButtonVisibility(false);
            }
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

        private async Task ShowNotSqlDialog(string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Not an SQL command",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                Content = message
            };

            await dialog.ShowAsync();
        }

        private async Task ShowSqlErrorDialog(string sqlCommand, string errorMessage)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "SQL Error",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                Content = $"SQL Command: {sqlCommand}\n\n{errorMessage}"
            };

            await dialog.ShowAsync();
        }
    }

}
