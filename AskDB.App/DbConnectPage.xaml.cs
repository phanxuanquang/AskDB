using DatabaseAnalyzer;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using Gemini.NET;
using Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Exception = System.Exception;
using Uri = System.Uri;


namespace AskDB.App
{
    public sealed partial class DbConnectPage : Page
    {
        public static bool IsFirstEnter = true;
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
                var keyword = autoSuggestBox.Text.Trim();

                if (dbTypeCombobox.SelectedIndex != -1)
                {
                    if (StringTool.IsNull(keyword))
                    {
                        autoSuggestBox.Text = Extractor.GetEnumDescription((DatabaseType)dbTypeCombobox.SelectedItem);
                        autoSuggestBox.Focus(FocusState.Programmatic);

                        connectDbButton.IsEnabled = true;
                    }
                    else
                    {
                        var suggestion = Cache
                            .Get(k => !Validator.CanBeValidApiKey(k)
                                && (k.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) || k.Contains(keyword, StringComparison.OrdinalIgnoreCase)), keyword)
                            .FirstOrDefault();

                        if (suggestion != null)
                        {
                            autoSuggestBox.Text = suggestion;
                            autoSuggestBox.Focus(FocusState.Programmatic);
                            connectDbButton.IsEnabled = true;

                        }
                    }

                    e.Handled = true;
                }
            }
        }
        private void ConnectionStringBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (StringTool.IsNull((sender as AutoSuggestBox).Text))
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
            var keyword = sender.Text.Trim();
            EnableForwardButton(Analyzer.DbExtractor?.ConnectionString, keyword);

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (StringTool.IsNull(keyword))
                {
                    sender.ItemsSource = null;
                    connectDbButton.IsEnabled = false;
                    return;
                }

                var source = Cache
                    .Get(k => !Validator.CanBeValidApiKey(k)
                        && (k.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) || k.Contains(keyword, StringComparison.OrdinalIgnoreCase)), keyword);

                sender.ItemsSource = source;
                connectDbButton.IsEnabled = true;
            }
        }

        private void ApiKeyBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                var autoSuggestBox = sender as AutoSuggestBox;
                var keyword = autoSuggestBox.Text.Trim();

                connectDbButton.IsEnabled = !StringTool.IsNull(keyword);

                if (StringTool.IsNull(keyword))
                {
                    autoSuggestBox.ItemsSource = null;
                    connectDbButton.IsEnabled = false;
                    e.Handled = true;
                    return;
                }

                var suggestion = Cache
                    .Get(k => k.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) && Validator.CanBeValidApiKey(k))
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(suggestion))
                {
                    autoSuggestBox.Text = suggestion;
                    autoSuggestBox.Focus(FocusState.Programmatic);
                }

                connectGeminiButton.IsEnabled = !string.IsNullOrEmpty(suggestion);
                e.Handled = true;
            }
        }
        private void ApiKeyBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (StringTool.IsNull((sender as AutoSuggestBox).Text))
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
            var keyword = sender.Text.Trim();
            EnableForwardButton(Cache.ApiKey, keyword);

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                connectGeminiButton.IsEnabled = true;
                if (StringTool.IsNull(keyword))
                {
                    sender.ItemsSource = null;
                    connectGeminiButton.IsEnabled = false;
                    return;
                }

                var source = Cache.Get(k => k.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) && Validator.CanBeValidApiKey(k));
                sender.ItemsSource = source;
            }
        }

        private async void DbTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dbTypeCombobox.SelectedIndex != -1)
            {
                var defaultConnectionString = Extractor.GetEnumDescription((DatabaseType)dbTypeCombobox.SelectedItem);

                connectionStringBox.PlaceholderText = defaultConnectionString;
                await Cache.Set(defaultConnectionString);

                connectDbButton.IsEnabled = true;
            }
            else
            {
                connectDbButton.IsEnabled = false;
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }
        private async void ConnectGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            WinUiHelper.SetLoading(true, sender as Button, apiKeyInputLoadingOverlay, apiInputPanel, "Validating your API key . . .");
            tutorialButton.Visibility = Visibility.Collapsed;

            var generator = new Generator(apiKeyBox.Text);
            var isValidApiKey = await generator.IsValidApiKeyAsync();

            if (isValidApiKey)
            {
                await Cache.Set(apiKeyBox.Text);
                step1Expander.IsExpanded = false;
                step1Expander.IsEnabled = true;
                step2Expander.IsExpanded = step2Expander.IsEnabled = true;
                Cache.ApiKey = apiKeyBox.Text.Trim();
            }
            else
            {
                await Cache.Remove(apiKeyBox.Text);
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Invalid API Key. Please try again.");
            }

            WinUiHelper.SetLoading(false, sender as Button, apiKeyInputLoadingOverlay, apiInputPanel);
            tutorialButton.Visibility = Visibility.Visible;
        }
        private async void ConnectDbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WinUiHelper.SetLoading(true, sender as Button, dbInputLoadingOverlay, dbInputPanel, "Loading database structure . . .");
                var selectedType = (DatabaseType)dbTypeCombobox.SelectedItem;

                Analyzer.DbExtractor = selectedType switch
                {
                    DatabaseType.SqlServer => new SqlServerExtractor(connectionStringBox.Text.Trim()),
                    DatabaseType.PostgreSQL => new PostgreSqlExtractor(connectionStringBox.Text.Trim()),
                    DatabaseType.SQLite => new SqliteExtractor(connectionStringBox.Text.Trim()),
                    DatabaseType.MySQL => new MySqlExtractor(connectionStringBox.Text.Trim()),
                    _ => throw new NotSupportedException("Not Supported"),
                };
                await Analyzer.DbExtractor.ExtractTables();
                Analyzer.DbExtractor.Tables = [.. Analyzer.DbExtractor.Tables.Where(t => t.Columns.Count > 0).OrderBy(t => t.Name)];

                if (Analyzer.DbExtractor.Tables.Count == 0)
                {
                    await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please ensure that your database has at least one table with data (not including system tables).", "Empty Database!");
                }
                else
                {
                    await Cache.Set(connectionStringBox.Text);

                    tablesListView.ItemsSource = Analyzer.DbExtractor.Tables.Select(t => t.Name);
                    step2Expander.IsExpanded = false;
                    step3Expander.IsExpanded = step3Expander.IsEnabled = true;
                    selectAllCheckbox.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                await Cache.Remove(connectionStringBox.Text);
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
            }
            finally
            {
                WinUiHelper.SetLoading(false, sender as Button, dbInputLoadingOverlay, dbInputPanel);
            }
        }

        private async void DbConnectPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dbTypeCombobox.ItemsSource = Enum.GetValues(typeof(DatabaseType));

                if (IsFirstEnter)
                {
                    await Cache.Init();
                    var apiKey = Cache.Get(Validator.CanBeValidApiKey).FirstOrDefault();

                    apiKeyBox.Text = apiKey;
                    connectGeminiButton.IsEnabled = !string.IsNullOrEmpty(apiKey);

                    await CheckUpdate();
                }
                else
                {
                    forwardButton.IsEnabled = true;
                    step3Expander.IsEnabled = true;

                    step1Expander.IsExpanded = step2Expander.IsExpanded = step3Expander.IsExpanded = true;

                    dbTypeCombobox.SelectedItem = Analyzer.DbExtractor.DatabaseType;

                    connectDbButton.IsEnabled = true;
                    startBtn.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
            }
        }
        private void TablesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsFirstEnter)
            {
                forwardButton.IsEnabled = false;
            }

            selectAllCheckbox.IsChecked = tablesListView.SelectedItems.Count == Analyzer.DbExtractor.Tables.Count;

            var selectedTableNames = tablesListView.SelectedItems.Cast<string>();
            var totalColumns = Analyzer.DbExtractor.Tables.Where(t => selectedTableNames.Contains(t.Name)).Sum(table => table.Columns.Count);
            if (totalColumns > Analyzer.MaxTotalColumns || tablesListView.SelectedItems.Count > Analyzer.MaxTotalTables)
            {
                startBtn.IsEnabled = false;
            }
            else
            {
                startBtn.IsEnabled = true;
            }
        }

        private void SelectAllCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFirstEnter)
            {
                forwardButton.IsEnabled = false;
            }
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

                if (selectedTableNames.Count() > 10)
                {
                    var dkm = new ContentDialog
                    {
                        XamlRoot = RootGrid.XamlRoot,
                        Title = "Warning",
                        Content = "Selecting too many tables may lead to inappropriate query suggestions, increasing analysis time, decreasing result quality, and unexpected processing errors.\n\nFor the best result, you should not select more than 10 tables, and make sure that you only select the necessary tables.",
                        PrimaryButtonText = "Cancel",
                        SecondaryButtonText = "Continue",
                        DefaultButton = ContentDialogButton.Primary
                    };

                    if (await dkm.ShowAsync() == ContentDialogResult.Primary)
                    {
                        return;
                    }
                }

                Analyzer.SelectedTables = Analyzer.DbExtractor.Tables.Where(t => selectedTableNames.Contains(t.Name)).ToList();
                MainPage.IsFirstEnter = true;
                IsFirstEnter = false;
                Frame.Navigate(typeof(MainPage), null, new EntranceNavigationTransitionInfo());
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
            }
        }

        private void EnableForwardButton(string newData, string oldData)
        {
            if (!IsFirstEnter)
            {
                forwardButton.IsEnabled = !string.IsNullOrEmpty(oldData) && newData.Equals(oldData);
            }
        }

        private async Task CheckUpdate()
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var latestRelease = await Extractor.GetGithubLatestReleaseInfo();

            if (latestRelease != null && !currentVersion.ToString().StartsWith(latestRelease.Name))
            {
                var sb = new StringBuilder();
                sb.AppendLine($"The latest version {latestRelease.Name} was released on {latestRelease.CreatedAt.ToString("MMMM dd, yyyy")}.");
                sb.AppendLine();
                sb.AppendLine("Please check the description of the latest version below:");
                sb.AppendLine(latestRelease.Body);
                sb.AppendLine();
                sb.AppendLine($"It is recommended to use the latest version for smoothest experience.");

                var updateConfirmation = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "New Version!",
                    Content = sb.ToString().Trim(),
                    PrimaryButtonText = "Download Now",
                    CloseButtonText = "Skip",
                    DefaultButton = ContentDialogButton.Primary
                };

                if (await updateConfirmation.ShowAsync() == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri(latestRelease.Assets[0].BrowserDownloadUrl));
                    Application.Current.Exit();
                }
            }
        }
    }
}
