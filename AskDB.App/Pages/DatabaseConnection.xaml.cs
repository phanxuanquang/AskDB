using AskDB.App.Helpers;
using AskDB.App.Pages;
using AskDB.App.View_Models;
using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Database;
using AskDB.Database.Extensions;
using AskDB.Database.Models;
using DatabaseInteractor.Factories;
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
        private bool _includePort = false;
        private DatabaseCredential _connectionCredential = new();

        public bool UseConnectionString
        {
            get => _useConnectionString;
            set
            {
                _useConnectionString = value;

                OnPropertyChanged();

                NotSqliteComponents.Visibility = VisibilityHelper.SetVisible(!_useConnectionString);
                SqliteComponents.Visibility = VisibilityHelper.SetVisible(false);
            }
        }
        public bool UseWindowsAuthentication
        {
            get => _useWindowsAuthentication;
            set
            {
                _useWindowsAuthentication = value;

                OnPropertyChanged();

                if (_useWindowsAuthentication)
                {
                    ConnectionCredential.Username = ConnectionCredential.Password = string.Empty;
                }
            }
        }
        public bool IncludePort
        {
            get => _includePort;
            set
            {
                _includePort = value;
                OnPropertyChanged();
                if (!_includePort)
                {
                    ConnectionCredential.Port = default;
                }
            }
        }
        public DatabaseCredential ConnectionCredential
        {
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
                _databaseTypes = [.. value];
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
                    var databaseInteractor = ServiceFactory.CreateInteractionService(ConnectionCredential.DatabaseType, connectionString);

                    await databaseInteractor.EnsureDatabaseConnectionAsync();
                    await _db.SaveConnectionStringAsync(ConnectionString);
                }
                else
                {
                    if (ConnectionCredential.DatabaseType == DatabaseType.SQLite && string.IsNullOrWhiteSpace(_sqliteFilePath))
                    {
                        await DialogHelper.ShowErrorAsync("Please select the SQLite database file.");
                        return;
                    }

                    var databaseInteractor = ServiceFactory.CreateInteractionService(ConnectionCredential.DatabaseType, ConnectionCredential.BuildConnectionString(5));

                    await databaseInteractor.EnsureDatabaseConnectionAsync();
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

            if (!_useConnectionString)
            {
                var isSqliteSelected = ConnectionCredential.DatabaseType == DatabaseType.SQLite;
                NotSqliteComponents.Visibility = VisibilityHelper.SetVisible(!isSqliteSelected);
                SqliteComponents.Visibility = VisibilityHelper.SetVisible(isSqliteSelected);
            }
        }
    }
}
