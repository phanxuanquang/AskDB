using AskDB.Commons.Extensions;
using AskDB.Database.Extensions;
using AskDB.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AskDB.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public static readonly string DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AskDb", "AskDb-v0.0.1.sqlite");

        #region Tables
        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<ConnectionString> ConnectionStrings { get; set; }
        public DbSet<Prompt> Prompts { get; set; }
        public DbSet<DatabaseCredential> DatabaseCredentials { get; set; }
        #endregion

        public async Task UpdateApiKeyAsync(string apiKey)
        {
            var userProfile = await UserSettings.FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return;
            }

            userProfile.ApiKey = apiKey.AesEncrypt();
            await SaveChangesAsync();
        }

        public async Task CreateUserAsync(string apiKey)
        {
            var user = new UserSetting
            {
                ApiKey = apiKey.AesEncrypt(),
            };

            await UserSettings.AddAsync(user);
            await SaveChangesAsync();
        }

        public async Task<string?> GetApiKeyAsync()
        {
            var userProfile = await UserSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return null;
            }

            return userProfile.ApiKey.AesDecrypt();
        }

        public async Task SaveDatabaseCredentialAsync(DatabaseCredential credential)
        {
            await DatabaseCredentials.AddAsync(credential.Encrypt());
            await SaveChangesAsync();
        }
    }
}
