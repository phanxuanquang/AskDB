using DatabaseAnalyzer;
using DatabaseAnalyzer.Models;
using GenAI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;


namespace AskDB.App
{
    public sealed partial class DbConnectPage : Page
    {
        public DbConnectPage()
        {
            this.InitializeComponent();
            this.Loaded += DbConnectPage_Loaded;
            connectGeminiButton.Click += ConnectGeminiButton_Click;
            connectDbButton.Click += ConnectDbButton_Click;
            getApiKeyButton.Click += GetApiKeyButton_Click;
            tablesListView.SelectionChanged += TablesListView_SelectionChanged;
        }

        private async void GetApiKeyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://aistudio.google.com/app/apikey") { UseShellExecute = true });
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

        private async void ConnectGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(apiKeyBox.Text) || string.IsNullOrWhiteSpace(apiKeyBox.Text))
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = RootGrid.XamlRoot;
                dialog.Title = "Error";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "You have not input your API Key.";

                await dialog.ShowAsync();
                return;
            }

            try
            {
                await Generator.GenerateContent(apiKeyBox.Text, "Say 'Hello World' to me!", false, CreativityLevel.Low);
            }
            catch
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = RootGrid.XamlRoot;
                dialog.Title = "Error";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Invalid API Key. Please try again.";

                await dialog.ShowAsync();
                return;
            }

            step1Expander.IsExpanded = step1Expander.IsEnabled = false;
            step2Expander.IsExpanded = step2Expander.IsEnabled = true;
            Analyzer.ApiKey = apiKeyBox.Text;
        }

        private async void ConnectDbButton_Click(object sender, RoutedEventArgs e)
        {
            if (dbTypeCombobox.SelectedIndex == -1)
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = RootGrid.XamlRoot;
                dialog.Title = "Error";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Please choose your database type.";

                await dialog.ShowAsync();
                return;
            }

            if (string.IsNullOrEmpty(connectionStringBox.Text) || string.IsNullOrWhiteSpace(connectionStringBox.Text))
            {
                ContentDialog dialog = new ContentDialog();

                dialog.XamlRoot = RootGrid.XamlRoot;
                dialog.Title = "Error";
                dialog.PrimaryButtonText = "OK";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Please input the connection string to connect to your database.";

                await dialog.ShowAsync();
                return;
            }

            try
            {
                var selectedType = (DatabaseType)dbTypeCombobox.SelectedItem;
                var tables = await Analyzer.GetTables(selectedType, connectionStringBox.Text);
                tablesListView.ItemsSource = tables.Select(t => t.Name).ToList();
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

            step2Expander.IsExpanded = step2Expander.IsEnabled = false;
            step3Expander.IsExpanded = step3Expander.IsEnabled = true;
            Analyzer.ConnectionString = connectionStringBox.Text;
            Analyzer.DatabaseType = (DatabaseType)dbTypeCombobox.SelectedItem;
        }

        private void DbConnectPage_Loaded(object sender, RoutedEventArgs e)
        {
            var dbtypes = (DatabaseType[])Enum.GetValues(typeof(DatabaseType));
            dbTypeCombobox.ItemsSource = dbtypes;
        }

        private void TablesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tablesListView.SelectedItems.Count > 0)
            {
                startBtn.IsEnabled = true;
            }
            else
            {
                startBtn.IsEnabled = false;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tables = await Analyzer.GetTables(Analyzer.DatabaseType, Analyzer.ConnectionString);
                var selectedTableNames = tablesListView.SelectedItems.Select(t => t.ToString()).ToList();
                Analyzer.Tables = tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();

                Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
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
    }
}
