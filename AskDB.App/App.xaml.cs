using AskDB.App.Helpers;
using AskDB.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
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

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitializeAsync();

            Window = new MainWindow();
            Window.Activate();
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

        private static async Task InitializeAsync()
        {
            if (!File.Exists(AppDbContext.DbPath))
            {
                var directory = Path.GetDirectoryName(AppDbContext.DbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            await LocalDb.Database.EnsureCreatedAsync().ConfigureAwait(false);

            Cache.ApiKey = await LocalDb.GetApiKeyAsync().ConfigureAwait(false);
            Cache.HasUserEverConnectedToDatabase = await LocalDb.IsDatabaseCredentialOrConnectionStringExistsAsync();
        }
    }
}
