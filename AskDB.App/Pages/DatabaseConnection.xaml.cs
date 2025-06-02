using AskDB.App.Helpers;
using AskDB.App.Pages;
using AskDB.App.View_Models;
using AskDB.Commons.Attributes;
using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Database;
using AskDB.Database.Extensions;
using AskDB.Database.Models;
using DatabaseInteractor.Services;
using DatabaseInteractor.Services.Extractors;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage.Pickers;

namespace AskDB.App
{
    public sealed partial class DatabaseConnection : Page, INotifyPropertyChanged
    {

        private List<string> _databaseTypes = [.. EnumExtensions.GetValues<DatabaseType>().Select(x => x.GetDescription())];
        private string _sqliteFilePath;
        private bool _useConnectionString = false;
        private bool _useWindowsAuthentication = false;
        private DatabaseCredential _connectionCredential = new();

        public bool UseWindowsAuthentication {
            get => _useWindowsAuthentication; 
            set
            {
                if (_useWindowsAuthentication != value)
                {
                    _useWindowsAuthentication = value;

                    OnPropertyChanged();
                }
            }
        }
        public DatabaseCredential ConnectionCredential { 
            get => _connectionCredential; 
            set 
            {
                _connectionCredential = value;
                OnPropertyChanged();
            } 
        }

        public ObservableCollection<string> DatabaseTypes
        {
            get => new(_databaseTypes);
            set
            {
                _databaseTypes = value.ToList();
                OnPropertyChanged();
            }
        }
        private ConnectionString ConnectionString { get; set; } = new();

        private readonly AppDbContext _db;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DatabaseConnection()
        {
            _db = App.LocalDb;
            UseWindowsAuthentication = false;
            InitializeComponent();
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
                if (_useConnectionString)
                {
                    var connectionString = ConnectionString.Value?.Trim();
                    var extractor = ConnectionCredential.DatabaseType switch
                    {
                        DatabaseType.SqlServer => (ExtractorBase)new SqlServerExtractor(connectionString),
                        DatabaseType.MySQL => new MySqlExtractor(connectionString),
                        DatabaseType.PostgreSQL => new PostgreSqlExtractor(connectionString),
                        DatabaseType.SQLite => new SqliteExtractor(connectionString),
                        _ => throw new NotImplementedException(),
                    };
                    await extractor.EnsureDatabaseConnectionAsync();
                    await _db.SaveConnectionStringAsync(ConnectionString);
                }
                else
                {
                    if (ConnectionCredential.DatabaseType == DatabaseType.SQLite && string.IsNullOrWhiteSpace(_sqliteFilePath))
                    {
                        await DialogHelper.ShowErrorAsync("Please select the SQLite database file.");
                        return;
                    }

                    var extractor = ConnectionCredential.DatabaseType switch
                    {
                        DatabaseType.SqlServer => (ExtractorBase)new SqlServerExtractor(ConnectionCredential.BuildConnectionString(5)),
                        DatabaseType.MySQL => new MySqlExtractor(ConnectionCredential.BuildConnectionString(5)),
                        DatabaseType.PostgreSQL => new PostgreSqlExtractor(ConnectionCredential.BuildConnectionString(5)),
                        DatabaseType.SQLite => new SqliteExtractor(_sqliteFilePath),
                        _ => throw new NotImplementedException(),
                    };

                    await extractor.EnsureDatabaseConnectionAsync();

                    await _db.SaveDatabaseCredentialAsync(ConnectionCredential);
                }

                Frame.Navigate(
                    typeof(ChatWithDatabase),
                    new DatabaseConnectionInfo
                    {
                        ConnectionString = _useConnectionString ? ConnectionString.Value?.Trim() : ConnectionCredential.BuildConnectionString(),
                        DatabaseType = ConnectionCredential.DatabaseType,
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

            ConnectionCredential.Port = ConnectionCredential.Port == default 
                ? ConnectionCredential.DatabaseType.GetAttributeValue<DefaultPortAttribute>().Port
                : ConnectionCredential.Port;

            ConnectionCredential.Host = string.IsNullOrWhiteSpace(ConnectionCredential.Host)
                ? ConnectionCredential.DatabaseType.GetAttributeValue<DefaultHostAttribute>().Host
                : ConnectionCredential.Host;

            if (!_useConnectionString)
            {
                var isSqliteSelected = ConnectionCredential.DatabaseType == DatabaseType.SQLite;
                NotSqliteComponents.Visibility = VisibilityHelper.SetVisible(!isSqliteSelected);
                SqliteComponents.Visibility = VisibilityHelper.SetVisible(isSqliteSelected);
            }
        }

        private void UseConnectionStringCheckBox_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _useConnectionString = (sender as CheckBox).IsChecked == true;

            UseConnectionStringSpace.Visibility = VisibilityHelper.SetVisible(_useConnectionString);
            NotSqliteComponents.Visibility = VisibilityHelper.SetVisible(!_useConnectionString);
        }

        private void UseWindowsAuthenticationCheckBox_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var isChecked = (sender as CheckBox).IsChecked == true;

            UseWindowsAuthentication = isChecked;
            if (isChecked)
            {
                ConnectionCredential.Username = ConnectionCredential.Password = null;
            }
        }
    }
}
