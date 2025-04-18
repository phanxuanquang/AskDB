using DatabaseAnalyzer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class QuerySuggestion : Page
    {
        public QuerySuggestion()
        {
            this.InitializeComponent();

            Loaded += TableSelection_Loaded;
            selectAllCheckbox.Click += SelectAllCheckbox_Click;
            tableSearchBox.TextChanged += TableSearchBox_TextChanged;
            startButton.Click += StartButton_Click;
            skipButton.Click += SkipButton_Click;
            backButton.Click += BackButton_Click;
        }

        private void TableSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = (sender as TextBox).Text.Trim();

            tablesListView.ItemsSource = Analyzer.DbExtractor.Tables
                .Select(t => t.Name)
                .Where(t => t.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPanel), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
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
                Frame.Navigate(typeof(MainPanel), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
            }
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

        private async void TableSelection_Loaded(object sender, RoutedEventArgs e)
        {
            if (Analyzer.DbExtractor.Tables.Count == 0)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please ensure that your database has at least one table with data (not including system tables).", "Empty Database!");
                Frame.GoBack();
            }
            else
            {
                tablesListView.ItemsSource = Analyzer.DbExtractor.Tables.Select(t => t.Name);
                selectAllCheckbox.IsChecked = false;
            }
        }
    }
}
