using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.App.ViewModels;
using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Database;
using AskDB.Database.Extensions;
using DatabaseInteractor.Services;
using DatabaseInteractor.Services.Extractors;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AskDB.App.Pages
{
    public sealed partial class ExistingDatabaseConnection : Page
    {
        private ObservableCollection<ExistingDatabaseConnectionInfo> ExistingDatabaseConnectionInfors = [];
        private ObservableCollection<ExistingConnectionStringInfor> ExistingConnectionStringInfors = [];

        private readonly AppDbContext _db;

        public ExistingDatabaseConnection()
        {
            InitializeComponent();
            _db = App.GetService<AppDbContext>();

        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.SetLoading("Connecting...", isLoading);
            LoadingOverlay.Visibility = VisibilityHelper.SetVisible(isLoading);
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SetLoading(true);

            var existingCredentials = await _db.GetDatabaseCredentialsAsync();
            var existingConnectionStrings = await _db.GetConnectionStringsAsync();

            if (existingCredentials.Count > 0)
            {
                ExistingDatabaseConnectionInfors = new ObservableCollection<ExistingDatabaseConnectionInfo>(existingCredentials.Select(credential => new ExistingDatabaseConnectionInfo
                {
                    Id = credential.Id,
                    Host = credential.Host,
                    Database = credential.Database,
                    DatabaseType = credential.DatabaseType,
                    DatabaseTypeDisplayName = credential.DatabaseType.GetDescription(),
                    LastAccess = credential.LastAccessTime,
                    ConnectionString = credential.BuildConnectionString()
                }));
                ConnectionCredentialsPanel.Visibility = VisibilityHelper.SetVisible(true);
            }
            else
            {
                ConnectionCredentialsPanel.Visibility = VisibilityHelper.SetVisible(false);
            }

            if (existingConnectionStrings.Count > 0)
            {
                ExistingConnectionStringInfors = new ObservableCollection<ExistingConnectionStringInfor>(existingConnectionStrings.Select(x => new ExistingConnectionStringInfor
                {
                    Id = x.Id,
                    Name = x.Name,
                    Value = x.Value,
                    DatabaseType = x.DatabaseType,
                    DatabaseTypeDisplayName = x.DatabaseType.GetDescription(),
                    LastAccess = x.LastAccessTime
                }));
                ConnectionStringsPanel.Visibility = VisibilityHelper.SetVisible(true);
            }
            else
            {
                ConnectionStringsPanel.Visibility = VisibilityHelper.SetVisible(false);
            }

            SetLoading(false);
        }

        private void SkipButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private async void DatabaseCredentionItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            var data = args.InvokedItem as ExistingDatabaseConnectionInfo;

            if (data == null)
            {
                return;
            }

            try
            {
                var extractorForConnectionString = data.DatabaseType switch
                {
                    DatabaseType.SqlServer => (ExtractorBase)new SqlServerExtractor(data.ConnectionString),
                    DatabaseType.MySQL => new MySqlExtractor(data.ConnectionString),
                    DatabaseType.PostgreSQL => new PostgreSqlExtractor(data.ConnectionString),
                    DatabaseType.SQLite => new SqliteExtractor(data.ConnectionString),
                    _ => throw new NotImplementedException(),
                };

                await extractorForConnectionString.EnsureDatabaseConnectionAsync();

                this.Frame.Navigate(typeof(ChatWithDatabase), new DatabaseConnectionInfo
                {
                    ConnectionString = data.ConnectionString,
                    DatabaseType = data.DatabaseType
                }, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                ExistingDatabaseConnectionInfors.Remove(data);
                await _db.RemoveDatabaseCredentialAsync(data.Id);
                await DialogHelper.ShowErrorAsync(ex.Message);
            }
        }

        private async void ConnectionStringItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            var data = args.InvokedItem as ExistingConnectionStringInfor;

            if (data == null)
            {
                return;
            }

            try
            {
                var extractorForConnectionString = data.DatabaseType switch
                {
                    DatabaseType.SqlServer => (ExtractorBase)new SqlServerExtractor(data.Value),
                    DatabaseType.MySQL => new MySqlExtractor(data.Value),
                    DatabaseType.PostgreSQL => new PostgreSqlExtractor(data.Value),
                    DatabaseType.SQLite => new SqliteExtractor(data.Value),
                    _ => throw new NotImplementedException(),
                };

                await extractorForConnectionString.EnsureDatabaseConnectionAsync();

                this.Frame.Navigate(typeof(ChatWithDatabase), new DatabaseConnectionInfo
                {
                    ConnectionString = data.Value,
                    DatabaseType = data.DatabaseType
                }, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                ExistingConnectionStringInfors.Remove(data);
                await _db.RemoveConnectionStringAsync(data.Id);
                await DialogHelper.ShowErrorAsync(ex.Message);
            }
        }
    }
}
