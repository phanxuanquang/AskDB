using AskDB.App.Helpers;
using AskDB.App.ViewModels;
using DatabaseAnalyzer;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using Helper;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.Storage.Pickers;

namespace AskDB.App
{
    public sealed partial class DatabaseConnection : Page
    {
        private DatabaseConnectionCredential _dbCredential { get; set; } = new();
        private bool _useWindowsAuthentication = true;
        private bool _enableSslTls = true;
        private string _defaultUsername = string.Empty;

        public DatabaseConnection()
        {
            InitializeComponent();
            SetDefaultValuesFor(_dbCredential.DatabaseType);
            CustomAuthenticationSpace.Visibility = VisibilityHelper.SetVisible(!_useWindowsAuthentication);
            SetLoading(false);

            DatabaseTypeComboBox.SelectionChanged += DatabaseTypeComboBox_SelectionChanged;
            BrowseButton.Click += BrowseButton_Click;
            ContinueButton.Click += ContinueButton_Click;
        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.SetLoading("Connecting...", isLoading);
            LoadingOverlay.Visibility = VisibilityHelper.SetVisible(isLoading);
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
        }

        private async void ContinueButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_dbCredential.DatabaseType == DatabaseType.SQLite && string.IsNullOrWhiteSpace(databaseFileBox.Text))
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please select the database file.");
                return;
            }

            if (_dbCredential.DatabaseType != DatabaseType.SQLite)
            {
                if (_useWindowsAuthentication && (string.IsNullOrWhiteSpace(_dbCredential.Username) || string.IsNullOrWhiteSpace(_dbCredential.Password)))
                {
                    await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please input the username and the password.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_dbCredential.Server) || string.IsNullOrWhiteSpace(_dbCredential.Database))
                {
                    await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please fill all the fields.");
                    return;
                }
            }
            else if (string.IsNullOrWhiteSpace(databaseFileBox.Text))
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please select the database file.");
                return;
            }

            var connectionString = string.IsNullOrEmpty(ConnectionStringBox.Text)
                ? _dbCredential.DatabaseType switch
                {
                    DatabaseType.MySQL => $"Server={_dbCredential.Database};Database={_dbCredential.Database};Uid={_dbCredential.Username};Pwd={_dbCredential.Password};Connection Timeout=15;SslMode={(_enableSslTls ? "Required" : "None")};",
                    DatabaseType.PostgreSQL => $"Host={_dbCredential.Database};Database={_dbCredential.Database};Username={_dbCredential.Username};Password={_dbCredential.Password};Pooling=true;MinPoolSize=0;MaxPoolSize=100;CommandTimeout=15;Timeout=15;{(_enableSslTls ? "SSL Mode=Require;" : string.Empty)}Trust Server Certificate=true;",
                    DatabaseType.SqlServer => $"Server={_dbCredential.Database};Database={_dbCredential.Database};User Id={_dbCredential.Username};Password={_dbCredential.Password};Connection Timeout=15;{(_enableSslTls ? "Encrypt=True;" : string.Empty)}{(_useWindowsAuthentication ? "Integrated Security=true;" : string.Empty)}TrustServerCertificate=True;",
                    DatabaseType.SQLite => $"Data Source={databaseFileBox.Text}",
                    _ => throw new InvalidOperationException("Unsupported database type."),
                }
                : ConnectionStringBox.Text;

            ConnectionStringBox.Text = Cache.ConnectionString = connectionString;

            try
            {
                SetLoading(true);
                Analyzer.DbExtractor = _dbCredential.DatabaseType switch
                {
                    DatabaseType.SqlServer => new SqlServerExtractor(connectionString),
                    DatabaseType.PostgreSQL => new PostgreSqlExtractor(connectionString),
                    DatabaseType.SQLite => new SqliteExtractor(connectionString),
                    DatabaseType.MySQL => new MySqlExtractor(connectionString),
                    _ => throw new NotSupportedException("Not Supported"),
                };
                await Analyzer.DbExtractor.ExtractTables();
                Analyzer.DbExtractor.Tables = [.. Analyzer.DbExtractor.Tables.Where(t => t.Columns.Count > 0).OrderBy(t => t.Name)];

                ConnectionStringBox.Visibility = VisibilityHelper.SetVisible(false);
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
                ConnectionStringBox.Visibility = VisibilityHelper.SetVisible(true);
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

            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                databaseFileBox.Text = file.Path;
            }

            senderButton.IsEnabled = true;
        }

        private void DatabaseTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            _dbCredential.DatabaseType = (DatabaseType)comboBox.SelectedIndex;
            SetDefaultValuesFor(_dbCredential.DatabaseType);
        }

        private void SetDefaultValuesFor(DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.MySQL:
                    _defaultUsername = "root";
                    break;
                case DatabaseType.PostgreSQL:
                    _defaultUsername = "postgres";
                    break;
                case DatabaseType.SqlServer:
                    break;
                case DatabaseType.SQLite:
                    NotSqliteComponents.Visibility = VisibilityHelper.SetVisible(false);
                    SqliteComponents.Visibility = VisibilityHelper.SetVisible(true);
                    break;
            }

            NotSqliteComponents.Visibility = VisibilityHelper.SetVisible(type != DatabaseType.SQLite);
            SqliteComponents.Visibility = VisibilityHelper.SetVisible(type == DatabaseType.SQLite);
        }

        private void UseWindowsAuthenticationCheckbox_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            CustomAuthenticationSpace.Visibility = VisibilityHelper.SetVisible(!_useWindowsAuthentication);
        }
    }
}
