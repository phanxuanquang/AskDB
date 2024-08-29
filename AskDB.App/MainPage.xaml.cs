using DatabaseAnalyzer;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using Helper;
using Table = DatabaseAnalyzer.Models.Table;
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;
using System.Data.SqlClient;

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
                var savePicker = new FileSavePicker();

                nint windowHandle = WindowNative.GetWindowHandle(App.Window);
                InitializeWithWindow.Initialize(savePicker, windowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                savePicker.FileTypeChoices.Add("CSV", new[] { ".csv" });
                savePicker.SuggestedFileName = "Exported Data";

                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    Extractor.ExportData(DataTable, file.Path);
                }
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = RootGrid.XamlRoot;
                dialog.Title = "Error";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = ex.Message;

                await dialog.ShowAsync();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
        }

        private async void ShowSqlButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.XamlRoot = RootGrid.XamlRoot;
            dialog.Title = "SQL Query";
            dialog.PrimaryButtonText = "Copy";
            dialog.CloseButtonText = "Close";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = SqlQuery;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(SqlQuery);
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }
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
                Tables = await Analyzer.GetTables(Analyzer.DatabaseType, Analyzer.ConnectionString);
                tablesListView.ItemsSource = Tables.Select(t => t.Name).ToList();

                foreach (var t in Analyzer.Tables)
                {
                    tablesListView.SelectedItems.Add(t.Name);
                }
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = RootGrid.XamlRoot;
                dialog.Title = "Error";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = ex.Message;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Frame.Navigate(typeof(DbConnectPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var isQuerySql = true;
            LoadingOverlay.Visibility = Visibility.Visible;
            mainPanel.Visibility = exportButton.Visibility = showSqlButton.Visibility = Visibility.Collapsed;

            var sqlCommand = new SqlCommander();
            outputGridView.Columns.Clear();

            IDatabaseExtractor extractor = new SqlServerExtractor();
            selectTableExpander.IsExpanded = false;

            switch (Analyzer.DatabaseType)
            {
                case DatabaseType.MSSQL:
                    break;
                case DatabaseType.PostgreSQL:
                    extractor = new PostgreSqlExtractor();
                    break;
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    extractor = new MySqlExtractor();
                    break;
                case DatabaseType.SQLite:
                    extractor = new SqliteExtractor();
                    break;
            }

            try
            {
                DataTable = await extractor.GetData(Analyzer.ConnectionString, queryBox.Text);
                exportButton.Visibility = Visibility.Visible;
            }
            catch
            {
                isQuerySql = false;
            }

            if (!isQuerySql)
            {
                Analyzer.Tables = Tables.Where(t => tablesListView.SelectedItems.Contains(t.Name)).ToList();

                try
                {
                    sqlCommand = await Analyzer.GetSql(Analyzer.ApiKey, Analyzer.Tables, queryBox.Text, Analyzer.DatabaseType);
                }
                catch (Exception ex)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    mainPanel.Visibility = Visibility.Visible;
                    ContentDialog dialog = new ContentDialog();

                    dialog.XamlRoot = RootGrid.XamlRoot;
                    dialog.Title = "Error";
                    dialog.PrimaryButtonText = "OK";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    dialog.Content = ex.Message;

                    await dialog.ShowAsync();
                    return;
                }

                if (!sqlCommand.IsSql)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    mainPanel.Visibility = Visibility.Visible;
                    exportButton.Visibility = showSqlButton.Visibility = Visibility.Collapsed;
                    ContentDialog dialog = new ContentDialog();

                    dialog.XamlRoot = RootGrid.XamlRoot;
                    dialog.Title = "Not an SQL command";
                    dialog.PrimaryButtonText = "OK";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    dialog.Content = sqlCommand.Output;

                    await dialog.ShowAsync();
                    return;
                }

                SqlQuery = sqlCommand.Output;

                try
                {
                    DataTable = await extractor.GetData(Analyzer.ConnectionString, sqlCommand.Output);
                    exportButton.Visibility = showSqlButton.Visibility = Visibility.Visible;
                    exportButton.IsEnabled = showSqlButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    ContentDialog dialog = new ContentDialog();

                    dialog.XamlRoot = RootGrid.XamlRoot;
                    dialog.Title = "Not an SQL command";
                    dialog.PrimaryButtonText = "OK";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    dialog.Content = $"SQL Command: {sqlCommand.Output}\n\n{ex.Message}";
                    exportButton.Visibility = showSqlButton.Visibility = Visibility.Collapsed;
                    exportButton.IsEnabled = showSqlButton.IsEnabled = false;

                    await dialog.ShowAsync();
                }
            }

            for (int i = 0; i < DataTable.Columns.Count; i++)
            {
                outputGridView.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn()
                {
                    Header = DataTable.Columns[i].ColumnName,
                    Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                });
            }

            var collectionObjects = new System.Collections.ObjectModel.ObservableCollection<object>();
            foreach (DataRow row in DataTable.Rows)
            {
                collectionObjects.Add(row.ItemArray);
            }

            outputGridView.ItemsSource = collectionObjects;
            LoadingOverlay.Visibility = Visibility.Collapsed;
            mainPanel.Visibility = Visibility.Visible;
        }
    }
}
