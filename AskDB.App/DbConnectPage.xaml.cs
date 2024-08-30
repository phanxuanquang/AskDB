using DatabaseAnalyzer;
using DatabaseAnalyzer.Models;
using GenAI;
using Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;


namespace AskDB.App
{
    public sealed partial class DbConnectPage : Page
    {
        private List<Table> Tables = new List<Table>();
        private List<string> Keywords = new List<string>();

        public DbConnectPage()
        {
            this.InitializeComponent();
            this.Loaded += DbConnectPage_Loaded;

            tablesListView.SelectionChanged += TablesListView_SelectionChanged;

            apiKeyBox.KeyDown += ApiKeyBox_KeyDown;
            apiKeyBox.TextChanged += ApiKeyBox_TextChanged;
            connectionStringBox.KeyDown += ConnectionStringBox_KeyDown;
            connectionStringBox.TextChanged += ConnectionStringBox_TextChanged;

            connectGeminiButton.Click += ConnectGeminiButton_Click;
            connectDbButton.Click += ConnectDbButton_Click;
            getApiKeyButton.Click += GetApiKeyButton_Click;
            forwardButton.Click += ForwardButton_Click;
            startBtn.Click += StartButton_Click;
        }

        private void ConnectionStringBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrEmpty(sender.Text))
                {
                    return;
                }

                var suggestions = Keywords.Where(k => k.ToUpper().StartsWith(sender.Text.ToUpper())).Take(10).OrderBy(k => k).Select(t => StringEngineer.ReplaceLastOccurrence(sender.Text, sender.Text, t)).ToList();
                sender.ItemsSource = suggestions;
            }
        }

        private void ApiKeyBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (string.IsNullOrEmpty(sender.Text))
                {
                    return;
                }

                var suggestions = Keywords.Where(k => k.ToUpper().StartsWith(sender.Text.ToUpper())).Take(10).OrderBy(k => k).Select(t => StringEngineer.ReplaceLastOccurrence(sender.Text, sender.Text, t)).ToList();
                sender.ItemsSource = suggestions;
            }
        }

        private void ConnectionStringBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty((sender as AutoSuggestBox).Text))
            {
                return;
            }

            if (e.Key == VirtualKey.Enter)
            {
                ConnectDbButton_Click(sender, e);
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

        private void ApiKeyBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty((sender as AutoSuggestBox).Text))
            {
                return;
            }

            if (e.Key == VirtualKey.Enter)
            {
                ConnectGeminiButton_Click(sender, e);
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
                await ShowErrorDialog(ex.Message);
            }
        }

        private async void ConnectGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(apiKeyBox.Text))
            {
                await ShowErrorDialog("You have not entered your API Key yet.");
                return;
            }

            try
            {
                await ValidateApiKey(sender as Button);
                await Cache.SetContent(apiKeyBox.Text);
                UpdateUIAfterApiValidation();
            }
            catch
            {
                await ShowErrorDialog("Invalid API Key. Please try again.");
            }
        }

        private async void ConnectDbButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDatabaseInput())
            {
                return;
            }

            try
            {
                await ConnectToDatabase(sender as Button);
                await Cache.SetContent(connectionStringBox.Text);
                UpdateUIAfterDatabaseConnection();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(ex.Message);
            }
            finally
            {
                SetDatabaseLoadingState(false, sender as Button);
            }
        }

        private async void DbConnectPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDatabaseTypes();
            Keywords = (await Cache.GetContent()).Distinct().OrderBy(x => x).ToList();

            if (Analyzer.IsActivated)
            {
                LoadSavedSettings();
            }
        }

        private void TablesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            startBtn.IsEnabled = tablesListView.SelectedItems.Count > 0;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSelectedTables();
                Analyzer.IsActivated = true;
                Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(ex.Message);
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "Error",
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();
        }

        private async Task ValidateApiKey(Button sender)
        {
            SetApiKeyLoadingState(true, sender);
            try
            {
                await Generator.GenerateContent(apiKeyBox.Text, "Say 'Hello World' to me!", false, CreativityLevel.Low);
            }
            finally
            {
                SetApiKeyLoadingState(false, sender);
            }
        }

        private void UpdateUIAfterApiValidation()
        {
            step1Expander.IsExpanded = false;
            step1Expander.IsEnabled = true;
            step2Expander.IsExpanded = step2Expander.IsEnabled = true;
            Analyzer.ApiKey = apiKeyBox.Text;
        }

        private bool ValidateDatabaseInput()
        {
            if (dbTypeCombobox.SelectedIndex == -1)
            {
                ShowErrorDialog("Please choose your database type.").Wait();
                return false;
            }

            if (string.IsNullOrWhiteSpace(connectionStringBox.Text))
            {
                ShowErrorDialog("Please input the connection string to connect to your database.").Wait();
                return false;
            }

            return true;
        }

        private async Task ConnectToDatabase(Button sender)
        {
            SetDatabaseLoadingState(true, sender);
            var selectedType = (DatabaseType)dbTypeCombobox.SelectedItem;
            Tables = await Analyzer.GetTables(selectedType, connectionStringBox.Text.Trim());
            tablesListView.ItemsSource = Tables.Select(t => t.Name).ToList();
        }

        private void UpdateUIAfterDatabaseConnection()
        {
            step2Expander.IsExpanded = step2Expander.IsEnabled = false;
            step3Expander.IsExpanded = step3Expander.IsEnabled = true;
            Analyzer.ConnectionString = connectionStringBox.Text;
            Analyzer.DatabaseType = (DatabaseType)dbTypeCombobox.SelectedItem;
        }

        private void LoadDatabaseTypes()
        {
            dbTypeCombobox.ItemsSource = Enum.GetValues(typeof(DatabaseType));
        }

        private void LoadSavedSettings()
        {
            apiKeyBox.Text = Analyzer.ApiKey;
            connectionStringBox.Text = Analyzer.ConnectionString;
            dbTypeCombobox.SelectedItem = Analyzer.DatabaseType;
            step2Expander.IsEnabled = true;
            forwardButton.IsEnabled = true;
        }

        private void SaveSelectedTables()
        {
            var selectedTableNames = tablesListView.SelectedItems.Cast<string>().ToList();
            Analyzer.Tables = Tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();
        }

        private void SetApiKeyLoadingState(bool isLoading, Button sender)
        {
            apiInputPanel.Visibility = tutorialButton.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            apiKeyInputLoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            sender.IsEnabled = !isLoading;
        }

        private void SetDatabaseLoadingState(bool isLoading, Button sender)
        {
            dbInputLoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            sender.IsEnabled = !isLoading;
            dbInputPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
