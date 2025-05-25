using AskDB.App.Helpers;
using AskDB.App.Pages;
using AskDB.App.View_Models;
using AskDB.App.ViewModels;
using DatabaseInteractor.Models.Enums;
using DatabaseInteractor.Services;
using DatabaseInteractor.Services.Extractors;
using Helper;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage.Pickers;

namespace AskDB.App
{
    public sealed partial class DatabaseConnection : Page
    {
        private DatabaseConnectionCredential ConnectionCredential { get; set; } = new();
        private ObservableCollection<string> DatabaseTypes { get; set; } = new ObservableCollection<string>(Enum.GetValues(typeof(DatabaseType)).Cast<DatabaseType>().Select(x => Extractor.GetEnumDescription(x)));
        private string _sqliteFilePath;

        public DatabaseConnection()
        {
            InitializeComponent();
            SetLoading(false);
        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.SetLoading("Connecting...", isLoading);
            LoadingOverlay.Visibility = VisibilityHelper.SetVisible(isLoading);
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
        }

        private async void ContinueButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SetLoading(true);

            try
            {
                if (ConnectionCredential.DatabaseType == DatabaseType.SQLite && string.IsNullOrWhiteSpace(_sqliteFilePath))
                {
                    await DialogHelper.ShowErrorAsync("Please select the SQLite database file.");
                    return;
                }

                var extractor = ConnectionCredential.DatabaseType switch
                {
                    DatabaseType.SqlServer => (ExtractorBase)new SqlServerExtractor(ConnectionCredential.AsConnectionString(5)),
                    DatabaseType.MySQL => new MySqlExtractor(ConnectionCredential.AsConnectionString(5)),
                    DatabaseType.PostgreSQL => new PostgreSqlExtractor(ConnectionCredential.AsConnectionString(5)),
                    DatabaseType.SQLite => new SqliteExtractor(_sqliteFilePath),
                    _ => throw new NotImplementedException(),
                };

                await extractor.EnsureDatabaseConnectionAsync();

                Frame.Navigate(
                    typeof(ChatWithDatabase),
                    new DatabaseConnectionInfo
                    {
                        ConnectionString = ConnectionCredential.AsConnectionString(),
                        DatabaseType = extractor.DatabaseType,
                    },
                    new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void BrowseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var senderButton = sender as Button;
            senderButton.IsEnabled = false;
            var openPicker = new FileOpenPicker();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add(".db");
            openPicker.FileTypeFilter.Add(".sqlite");

            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                _sqliteFilePath = file.Path;
            }

            senderButton.IsEnabled = true;
        }

        private void DatabaseTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectionCredential.DatabaseType = (DatabaseType)(sender as ComboBox).SelectedIndex;

            var isSqliteSelected = ConnectionCredential.DatabaseType == DatabaseType.SQLite;

            NotSqliteComponents.Visibility = VisibilityHelper.SetVisible(!isSqliteSelected);
            SqliteComponents.Visibility = VisibilityHelper.SetVisible(isSqliteSelected);
        }
    }
}
