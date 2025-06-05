using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.Commons.Extensions;
using AskDB.Database;
using AskDB.Database.Extensions;
using DatabaseInteractor.Services;
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
            _db = App.LocalDb;
        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.SetLoading("Connecting", isLoading);
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
            ExistingDatabaseConnectionInfors.Clear();
            ExistingConnectionStringInfors.Clear();
            this.Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private async void DatabaseCredentionItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not ExistingDatabaseConnectionInfo data)
            {
                return;
            }

            LoadingOverlay.SetLoading("Connecting", true);

            try
            {
                var databaseInteractor = ServiceFactory.CreateInteractionService(data.DatabaseType, data.ConnectionString);

                await databaseInteractor.EnsureDatabaseConnectionAsync();

                this.Frame.Navigate(typeof(ChatWithDatabase), new DatabaseConnectionInfo
                {
                    ConnectionString = data.ConnectionString,
                    DatabaseType = data.DatabaseType
                }, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                var result = await DialogHelper.ShowDialogWithOptions("Error", ex.Message, "Remove permanently");

                if (result == ContentDialogResult.Primary)
                {
                    await _db.RemoveDatabaseCredentialAsync(data.Id);
                    ExistingDatabaseConnectionInfors.Remove(data);
                    if (ExistingDatabaseConnectionInfors.Count == 0)
                    {
                        ConnectionCredentialsPanel.Visibility = VisibilityHelper.SetVisible(false);
                    }
                }
            }
            finally
            {
                LoadingOverlay.SetLoading("Connecting", false);
            }
        }

        private async void ConnectionStringItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not ExistingConnectionStringInfor data)
            {
                return;
            }

            LoadingOverlay.SetLoading("Connecting", true);

            try
            {
                var databaseInteractor = ServiceFactory.CreateInteractionService(data.DatabaseType, data.Value);

                await databaseInteractor.EnsureDatabaseConnectionAsync();

                this.Frame.Navigate(typeof(ChatWithDatabase), new DatabaseConnectionInfo
                {
                    ConnectionString = data.Value,
                    DatabaseType = data.DatabaseType
                }, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            catch (Exception ex)
            {
                var result = await DialogHelper.ShowDialogWithOptions("Error", ex.Message, "Remove permanently");

                if (result == ContentDialogResult.Primary)
                {
                    await _db.RemoveConnectionStringAsync(data.Id);
                    ExistingConnectionStringInfors.Remove(data);
                    if (ExistingConnectionStringInfors.Count == 0)
                    {
                        ConnectionStringsPanel.Visibility = VisibilityHelper.SetVisible(false);
                    }
                }
            }
            finally
            {
                LoadingOverlay.SetLoading("Connecting", false);
            }
        }
    }
}
