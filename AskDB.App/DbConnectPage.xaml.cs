using DatabaseAnalyzer;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using GenAI;
using Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Data;
using System.Linq;
using Windows.System;


namespace AskDB.App
{
    public sealed partial class DbConnectPage : Page
    {
        public DbConnectPage()
        {
            this.InitializeComponent();
            this.Loaded += DbConnectPage_Loaded;

            tablesListView.SelectionChanged += TablesListView_SelectionChanged;
            dbTypeCombobox.SelectionChanged += DbTypeCombobox_SelectionChanged;

            selectAllCheckbox.Click += SelectAllCheckbox_Click;

            apiKeyBox.TextChanged += ApiKeyBox_TextChanged;
            apiKeyBox.KeyDown += ApiKeyBox_KeyDown;
            apiKeyBox.KeyUp += ApiKeyBox_KeyUp;

            connectionStringBox.TextChanged += ConnectionStringBox_TextChanged;
            connectionStringBox.KeyDown += ConnectionStringBox_KeyDown;
            connectionStringBox.KeyUp += ConnectionStringBox_KeyUp;

            connectGeminiButton.Click += ConnectGeminiButton_Click;
            connectDbButton.Click += ConnectDbButton_Click;
            forwardButton.Click += ForwardButton_Click;
            startBtn.Click += StartButton_Click;
        }

        private void ConnectionStringBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                connectDbButton.IsEnabled = true;
                var autoSuggestBox = sender as AutoSuggestBox;

                if (StringEngineer.IsNull(autoSuggestBox.Text))
                {
                    autoSuggestBox.ItemsSource = null;
                    connectDbButton.IsEnabled = false;
                    e.Handled = true;
                    return;
                }

                var suggestion = Cache.Data.FirstOrDefault(k => k.Contains(autoSuggestBox.Text, StringComparison.OrdinalIgnoreCase) && !Generator.CanBeGeminiApiKey(autoSuggestBox.Text));

                if (suggestion != null)
                {
                    autoSuggestBox.Text = suggestion;
                    autoSuggestBox.Focus(FocusState.Programmatic);
                }
                e.Handled = true;
            }
        }
        private void ConnectionStringBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (StringEngineer.IsNull((sender as AutoSuggestBox).Text))
                {
                    (sender as AutoSuggestBox).ItemsSource = null;
                    connectDbButton.IsEnabled = false;
                    return;
                }

                ConnectDbButton_Click(connectDbButton, e);
                e.Handled = true;
            }
        }
        private void ConnectionStringBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (StringEngineer.IsNull(sender.Text))
                {
                    sender.ItemsSource = null;
                    connectDbButton.IsEnabled = false;
                    return;
                }

                var source = Cache.Get(k => k.Contains(sender.Text, StringComparison.OrdinalIgnoreCase) && !Generator.CanBeGeminiApiKey(sender.Text));
                sender.ItemsSource = source;
                connectDbButton.IsEnabled = true;
            }
        }

        private void ApiKeyBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                var autoSuggestBox = sender as AutoSuggestBox;
                connectDbButton.IsEnabled = !StringEngineer.IsNull(autoSuggestBox.Text);
                if (StringEngineer.IsNull(autoSuggestBox.Text))
                {
                    autoSuggestBox.ItemsSource = null;
                    connectDbButton.IsEnabled = false;
                    e.Handled = true;
                    return;
                }
                var suggestion = Cache.Data.FirstOrDefault(k => !StringEngineer.IsNull(autoSuggestBox.Text) && k.StartsWith(autoSuggestBox.Text, StringComparison.OrdinalIgnoreCase) && Generator.CanBeGeminiApiKey(k));
                if (suggestion != null)
                {
                    autoSuggestBox.Text = suggestion;
                    autoSuggestBox.Focus(FocusState.Programmatic);
                }

                connectGeminiButton.IsEnabled = !StringEngineer.IsNull(autoSuggestBox.Text);
                e.Handled = true;
            }
        }
        private void ApiKeyBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (StringEngineer.IsNull((sender as AutoSuggestBox).Text))
                {
                    (sender as AutoSuggestBox).ItemsSource = null;
                    connectGeminiButton.IsEnabled = false;
                    return;
                }

                ConnectGeminiButton_Click(connectGeminiButton, e);
                e.Handled = true;
            }
        }
        private void ApiKeyBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                connectGeminiButton.IsEnabled = true;

                if (StringEngineer.IsNull(sender.Text))
                {
                    sender.ItemsSource = null;
                    connectGeminiButton.IsEnabled = false;
                    return;
                }

                var source = Cache.Get(k => k.StartsWith(sender.Text, StringComparison.OrdinalIgnoreCase) && Generator.CanBeGeminiApiKey(k));
                sender.ItemsSource = source;
            }
        }

        private void DbTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dbTypeCombobox.SelectedIndex != -1)
            {
                var defaultConnectionString = Extractor.GetEnumDescription((DatabaseType)dbTypeCombobox.SelectedItem);

                connectionStringBox.PlaceholderText = defaultConnectionString;
                Cache.Data.Add(defaultConnectionString);

                connectDbButton.IsEnabled = true;
            }
            else
            {
                connectGeminiButton.IsEnabled = false;
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }
        private async void ConnectGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            WinUiHelper.SetLoading(true, sender as Button, apiKeyInputLoadingOverlay, apiInputPanel);
            tutorialButton.Visibility = Visibility.Collapsed;

            var isValidApiKey = await Generator.IsValidApiKey(apiKeyBox.Text);

            if (isValidApiKey)
            {
                await Cache.Set(apiKeyBox.Text);
                step1Expander.IsExpanded = false;
                step1Expander.IsEnabled = true;
                step2Expander.IsExpanded = step2Expander.IsEnabled = true;
                Generator.ApiKey = apiKeyBox.Text;
            }
            else
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, "Invalid API Key. Please try again.");
            }

            WinUiHelper.SetLoading(false, sender as Button, apiKeyInputLoadingOverlay, apiInputPanel);
            tutorialButton.Visibility = Visibility.Visible;
        }
        private async void ConnectDbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WinUiHelper.SetLoading(true, sender as Button, dbInputLoadingOverlay, dbInputPanel);
                var selectedType = (DatabaseType)dbTypeCombobox.SelectedItem;

                switch (selectedType)
                {
                    case DatabaseType.SqlServer:
                        Analyzer.DatabaseExtractor = new SqlServerExtractor(connectionStringBox.Text.Trim());
                        break;
                    case DatabaseType.PostgreSQL:
                        Analyzer.DatabaseExtractor = new PostgreSqlExtractor(connectionStringBox.Text.Trim());
                        break;
                    case DatabaseType.SQLite:
                        Analyzer.DatabaseExtractor = new SqliteExtractor(connectionStringBox.Text.Trim());
                        break;
                    case DatabaseType.MySQL:
                        Analyzer.DatabaseExtractor = new MySqlExtractor(connectionStringBox.Text.Trim());
                        break;
                    default:
                        throw new NotSupportedException("Not Supported");
                }

                await Analyzer.DatabaseExtractor.ExtractTables();

                if (Analyzer.DatabaseExtractor.Tables.Count == 0)
                {
                    await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, "Cannot find any tables in this database", "Not Found");
                    WinUiHelper.SetLoading(false, sender as Button, dbInputLoadingOverlay, dbInputPanel);
                }
                else
                {
                    await Cache.Set(connectionStringBox.Text);

                    tablesListView.ItemsSource = Analyzer.DatabaseExtractor.Tables.Select(t => t.Name);
                    step2Expander.IsExpanded = step2Expander.IsEnabled = false;
                    step3Expander.IsExpanded = step3Expander.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
            }
            finally
            {
                WinUiHelper.SetLoading(false, sender as Button, dbInputLoadingOverlay, dbInputPanel);
            }
        }

        private async void DbConnectPage_Loaded(object sender, RoutedEventArgs e)
        {
            dbTypeCombobox.ItemsSource = Enum.GetValues(typeof(DatabaseType));
            await Cache.Init();

            if (Analyzer.IsActivated)
            {
                apiKeyBox.Text = Generator.ApiKey;
                connectionStringBox.Text = Analyzer.DatabaseExtractor.ConnectionString;
                dbTypeCombobox.SelectedItem = Analyzer.DatabaseExtractor.DatabaseType;
                step2Expander.IsEnabled = forwardButton.IsEnabled = connectDbButton.IsEnabled = connectGeminiButton.IsEnabled = true;
            }
        }

        private void TablesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            startBtn.IsEnabled = tablesListView.SelectedItems.Count > 0;
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
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                var selectedTableNames = tablesListView.SelectedItems.Cast<string>();
                Analyzer.SelectedTables = Analyzer.DatabaseExtractor.Tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();
                Analyzer.IsActivated = true;
                Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowErrorDialog(RootGrid.XamlRoot, ex.Message);
            }
        }
    }
}
