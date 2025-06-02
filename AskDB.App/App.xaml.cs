using AskDB.App.Helpers;
using AskDB.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AskDB.App
{
    public partial class App : Application
    {
        public static IHost Host { get; private set; }
        public static Window Window { get; private set; }
        public static AppDbContext LocalDb { get; private set; }

        public App()
        {
            this.InitializeComponent();
            Host = CreateHostBuilder();
            LocalDb = Host.Services.GetService<AppDbContext>();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Stopwatch.StartNew();
            Window = new MainWindow();
            Window.Activate();
            _ = InitializeAndLogAsync();
        }

        public static AppDbContext GetAppDbContextService()
        {
            return Host.Services.GetService<AppDbContext>() ?? throw new InvalidOperationException("Service AppDbContext not found.");
        }

        private static IHost CreateHostBuilder()
        {
            return Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={AppDbContext.DbPath}"));
                })
                .Build();
        }

        private static async Task InitializeAndLogAsync()
        {
            if (!File.Exists(AppDbContext.DbPath))
            {
                await LocalDb.Database.EnsureCreatedAsync().ConfigureAwait(false);
            }

            var apiKeyTask = LocalDb.GetApiKeyAsync();
            var hasUserTask = LocalDb.IsDatabaseCredentialOrConnectionStringExistsAsync();
            await Task.WhenAll(apiKeyTask, hasUserTask).ConfigureAwait(false);
            Cache.ApiKey = apiKeyTask.Result;
            Cache.HasUserEverConnectedToDatabase = hasUserTask.Result;
        }
    }
}
