using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer;
using DatabaseAnalyzer.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using System.Linq;
using Helper;

namespace AskDB.App
{
    public sealed partial class DatabaseConnection : Page
    {
        public DatabaseConnection()
        {
            InitializeComponent();
            SetDefaultValuesFor(DatabaseType.SqlServer);
            databaseTypeComboBox.SelectionChanged += DatabaseTypeComboBox_SelectionChanged;
            browseButton.Click += BrowseButton_Click;
            continueButton.Click += ContinueButton_Click;
            backButton.Click += BackButton_Click;
        }

        private void BackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private async void ContinueButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (databaseTypeComboBox.SelectedIndex == (int)DatabaseType.SQLite && string.IsNullOrWhiteSpace(databaseFileBox.Text))
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please select the database file.");
                return;
            }

            if (string.IsNullOrWhiteSpace(serverBox.Text) 
                || string.IsNullOrWhiteSpace(usernameBox.Text) 
                || string.IsNullOrWhiteSpace(passwordBox.Password) 
                || string.IsNullOrWhiteSpace(databaseBox.Text))
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, "Please fill all the fields.");
                return;
            }

            var databaseType = (DatabaseType)databaseTypeComboBox.SelectedIndex;

            var connectionString = databaseType switch
            {
                DatabaseType.MySQL => $"Server={serverBox.Text};Port={portBox.Text};Database={databaseBox.Text};Uid={usernameBox.Text};Pwd={passwordBox.Password};Connection Timeout={connectionTimeoutBox.Value};SslMode={(enableSslTlsCheckBox.IsChecked == true ? "Required" : "None")};",
                DatabaseType.PostgreSQL => $"Host={serverBox.Text};Port={portBox.Text};Username={usernameBox.Text};Password={passwordBox.Password};Database={databaseBox.Text};Pooling=true;MinPoolSize=0;MaxPoolSize=100;CommandTimeout={connectionTimeoutBox.Value};Timeout={connectionTimeoutBox.Value};{(enableSslTlsCheckBox.IsChecked == true ? "SSL Mode=Require;" : string.Empty)}Trust Server Certificate=true;",
                DatabaseType.SqlServer => $"Integrated Security=True;Server={serverBox.Text};Database={databaseBox.Text};Connection Timeout={connectionTimeoutBox.Value};{(enableSslTlsCheckBox.IsChecked == true ? "Encrypt=True;" : string.Empty)}{(authenticationBox.Text == "Windows Authentication" ? "Integrated Security=true;" : string.Empty)}TrustServerCertificate=True;",
                DatabaseType.SQLite => $"Data Source={databaseFileBox.Text}",
                _ => throw new InvalidOperationException("Unsupported database type."),
            };

            connectionStringBox.Text = Cache.ConnectionString = connectionString = "data source=kita.database.windows.net;initial catalog=StudMinDB;persist security info=True;user id=kita;password=ongnoipassword123@;MultipleActiveResultSets=True";
            
            try
            {
                Analyzer.DbExtractor = databaseType switch
                {
                    DatabaseType.SqlServer => new SqlServerExtractor(connectionString),
                    DatabaseType.PostgreSQL => new PostgreSqlExtractor(connectionString),
                    DatabaseType.SQLite => new SqliteExtractor(connectionString),
                    DatabaseType.MySQL => new MySqlExtractor(connectionString),
                    _ => throw new NotSupportedException("Not Supported"),
                };
                await Analyzer.DbExtractor.ExtractTables();
                Analyzer.DbExtractor.Tables = [.. Analyzer.DbExtractor.Tables.Where(t => t.Columns.Count > 0).OrderBy(t => t.Name)];

                connectionStringBox.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                Frame.Navigate(typeof(TableSelection), null, new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo() { Effect = Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                await WinUiHelper.ShowDialog(RootGrid.XamlRoot, ex.Message);
                connectionStringBox.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
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
            SetDefaultValuesFor((DatabaseType)(sender as ComboBox).SelectedIndex);
        }

        private void SetDefaultValuesFor(DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.MySQL:
                    portBox.PlaceholderText = "3306";
                    serverBox.PlaceholderText = "localhost";
                    usernameBox.PlaceholderText = "root";
                    break;
                case DatabaseType.PostgreSQL:
                    portBox.PlaceholderText = "5432";
                    serverBox.PlaceholderText = "localhost";
                    usernameBox.PlaceholderText = "postgres";
                    break;
                case DatabaseType.SqlServer:
                    portBox.PlaceholderText = "1433";
                    serverBox.PlaceholderText = "localhost";
                    break;
                case DatabaseType.SQLite:
                    notSqliteComponents.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    sqliteComponents.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    break;
            }

            if (type != DatabaseType.SQLite)
            {
                notSqliteComponents.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                sqliteComponents.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                notSqliteComponents.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                sqliteComponents.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
        }
    }
}
