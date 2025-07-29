using AskDB.App.Helpers;
using AskDB.App.View_Models;
using AskDB.Commons.Extensions;
using AskDB.Database;
using AskDB.Database.Extensions;
using DatabaseInteractor.Factories;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;

namespace AskDB.App.Pages
{
    public sealed partial class ExistingDatabaseConnection : Page
    {
        public ObservableCollection<ExistingDatabaseConnectionInfo> ExistingDatabaseConnectionInfors { get; set; } = [];
        public ObservableCollection<ExistingConnectionStringInfor> ExistingConnectionStringInfors { get; set; } = [];

        private readonly AppDbContext _db;

        public ExistingDatabaseConnection()
        {
            InitializeComponent();
            _db = App.LocalDb;
        }

        private void SetLoading(bool isLoading)
        {
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
            LoadingOverlay.SetLoading("Connecting", isLoading, 72);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SetLoading(true);
            ExistingDatabaseConnectionInfors.Clear();
            ExistingConnectionStringInfors.Clear();

            var existingCredentials = await _db.GetDatabaseCredentialsAsync();
            
            if (existingCredentials.Count > 0)
            {
                foreach (var credential in existingCredentials)
                {
                    ExistingDatabaseConnectionInfors.Add(new ExistingDatabaseConnectionInfo
                    {
                        Id = credential.Id,
                        Host = credential.Host,
                        Database = credential.Database,
                        DatabaseType = credential.DatabaseType,
                        DatabaseTypeDisplayName = credential.DatabaseType.GetDescription(),
                        LastAccess = credential.LastAccessTime,
                        ConnectionString = credential.BuildConnectionString()
                    });
                }
            }

            var existingConnectionStrings = await _db.GetConnectionStringsAsync();

            if (existingConnectionStrings.Count > 0)
            {
                foreach (var x in existingConnectionStrings)
                {
                    ExistingConnectionStringInfors.Add(new ExistingConnectionStringInfor
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Value = x.Value,
                        DatabaseType = x.DatabaseType,
                        DatabaseTypeDisplayName = x.DatabaseType.GetDescription(),
                        LastAccess = x.LastAccessTime
                    });
                }
            }

            SetLoading(false);
        }

        private void SkipButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ExistingDatabaseConnectionInfors.Clear();
            ExistingConnectionStringInfors.Clear();
            Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private async void DatabaseCredentionItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not ExistingDatabaseConnectionInfo data)
            {
                return;
            }

            SetLoading(true);

            try
            {
                var databaseInteractor = ServiceFactory.CreateInteractionService(data.DatabaseType, data.ConnectionString);

                await databaseInteractor.EnsureDatabaseConnectionAsync();

                Frame.Navigate(typeof(ChatWithDatabase),
                    new DatabaseConnectionInfo
                    {
                        ConnectionString = data.ConnectionString,
                        DatabaseType = data.DatabaseType
                    },
                    new SlideNavigationTransitionInfo()
                    {
                        Effect = SlideNavigationTransitionEffect.FromRight
                    });
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                var result = await DialogHelper.ShowDialogWithOptions("Error", ex.Message, "Remove permanently");

                if (result == ContentDialogResult.Primary)
                {
                    await _db.RemoveDatabaseCredentialAsync(data.Id);
                    ExistingDatabaseConnectionInfors.Remove(data);

                    if (ExistingDatabaseConnectionInfors.Count == 0 && ExistingConnectionStringInfors.Count == 0)
                    {
                        Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                    }
                }
            }
            finally
            {
                SetLoading(false);
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
                }
            }
            finally
            {
                LoadingOverlay.SetLoading("Connecting", false);
            }
        }
    }
}
