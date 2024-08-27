using DatabaseAnalyzer;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AskDB.App
{
    public sealed partial class MainPage : Page
    {
        private List<Table> Tables;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            queryBox.KeyDown += QueryBox_KeyDown;
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
            Tables = await Analyzer.GetTables(Analyzer.DatabaseType, Analyzer.ConnectionString);
            tablesListView.ItemsSource = Tables.Select(t => t.Name).ToList();

            foreach (var t in Analyzer.Tables)
            {
                tablesListView.SelectedItems.Add(t.Name);
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var sqlCommand = new SqlCommander();
            outputGridView.Columns.Clear();
            Analyzer.Tables = Tables.Where(t => tablesListView.SelectedItems.Contains(t.Name)).ToList();

            try
            {
                sqlCommand = await Analyzer.GetSql(Analyzer.ApiKey, Analyzer.Tables, queryBox.Text, Analyzer.DatabaseType);
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
                return;
            }

            if (!sqlCommand.IsSql)
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = RootGrid.XamlRoot;
                dialog.Title = "Not an SQL command";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = sqlCommand.Output;

                await dialog.ShowAsync();
                return;
            }

            IDatabaseExtractor extractor = new SqlServerExtractor();
            selectTableExpander.IsExpanded = true;

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

            var dataTable = await extractor.GetData(Analyzer.ConnectionString, sqlCommand.Output);

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                outputGridView.Columns.Add(new CommunityToolkit.WinUI.UI.Controls.DataGridTextColumn()
                {
                    Header = dataTable.Columns[i].ColumnName,
                    Binding = new Binding { Path = new PropertyPath("[" + i.ToString() + "]") }
                });
            }

            var collectionObjects = new System.Collections.ObjectModel.ObservableCollection<object>();
            foreach (DataRow row in dataTable.Rows)
            {
                collectionObjects.Add(row.ItemArray);
            }

            outputGridView.ItemsSource = collectionObjects;
        }
    }
}
