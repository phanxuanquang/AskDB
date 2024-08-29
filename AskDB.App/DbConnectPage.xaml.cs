using DatabaseAnalyzer;
using DatabaseAnalyzer.Models;
using GenAI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Popups;
using WinRT.Interop;
using Helper;


namespace AskDB.App
{
    public sealed partial class DbConnectPage : Page
    {
        private List<Table> Tables = new List<Table>();
        public DbConnectPage()
        {
            this.InitializeComponent();
            this.Loaded += DbConnectPage_Loaded;
            connectGeminiButton.Click += ConnectGeminiButton_Click;
            connectDbButton.Click += ConnectDbButton_Click;
            getApiKeyButton.Click += GetApiKeyButton_Click;
            forwardButton.Click += ForwardButton_Click;
            tablesListView.SelectionChanged += TablesListView_SelectionChanged;
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
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
                dialog.Content = "You have not entered your API Key yet.";

                await dialog.ShowAsync();
                return;
            }

            try
            {
                apiInputPanel.Visibility = tutorialButton.Visibility = Visibility.Collapsed;
                apiKeyInputLoadingOverlay.Visibility = Visibility.Visible;
                (sender as Button).IsEnabled = false;

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
            finally
            {
                apiKeyInputLoadingOverlay.Visibility = Visibility.Collapsed;
                (sender as Button).IsEnabled = true;

                apiInputPanel.Visibility = tutorialButton.Visibility = Visibility.Visible;
            }

            step1Expander.IsExpanded = false;
            step1Expander.IsEnabled = true;
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
                dbInputLoadingOverlay.Visibility = Visibility.Visible;
                (sender as Button).IsEnabled = false;
                dbInputPanel.Visibility = Visibility.Collapsed;

                var selectedType = (DatabaseType)dbTypeCombobox.SelectedItem;
                Tables = await Analyzer.GetTables(selectedType, connectionStringBox.Text);
                tablesListView.ItemsSource = Tables.Select(t => t.Name).ToList();
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
            finally
            {
                dbInputLoadingOverlay.Visibility = Visibility.Collapsed;
                (sender as Button).IsEnabled = true;
                dbInputPanel.Visibility = Visibility.Visible;
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

            if (Analyzer.IsActivated)
            {
                apiKeyBox.Text = Analyzer.ApiKey;
                connectionStringBox.Text = Analyzer.ConnectionString;
                dbTypeCombobox.SelectedItem = Analyzer.DatabaseType;

                step2Expander.IsEnabled = true;

                forwardButton.IsEnabled = true;
            }
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
                var selectedTableNames = tablesListView.SelectedItems.Select(t => t.ToString()).ToList();
                Analyzer.Tables = Tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();
                Analyzer.IsActivated = true;

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
