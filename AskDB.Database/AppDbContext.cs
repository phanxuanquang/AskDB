using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Database.Extensions;
using AskDB.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AskDB.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public static readonly string DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AskDb", "AskDb-v0.0.5.sqlite");

        #region Tables
        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<ConnectionString> ConnectionStrings { get; set; }
        public DbSet<DatabaseCredential> DatabaseCredentials { get; set; }
        #endregion

        #region API Key Configurations
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

        public async Task CreateOrUpdateApiKeyAsync(string apiKey)
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
            var apiKey = await UserSettings
                .AsNoTracking()
                .Select(x => x.ApiKey)
                .FirstOrDefaultAsync();

            if (apiKey == null)
            {
                return null;
            }

            return apiKey.AesDecrypt();
        }
        #endregion

        public async Task SaveDatabaseCredentialAsync(DatabaseCredential credential)
        {
            var model = credential.Encrypt();

            var existingCredential = await DatabaseCredentials.FirstOrDefaultAsync(c => c.Host == model.Host && c.Database == model.Database && c.Username == model.Username);

            if (existingCredential == null)
            {
                await DatabaseCredentials.AddAsync(model);
            }
            else
            {
                existingCredential.Port = model.Port;
                existingCredential.Password = model.Password;
                existingCredential.EnableTrustServerCertificate = model.EnableTrustServerCertificate;
                existingCredential.EnableSsl = model.EnableSsl;
                existingCredential.LastModifiedTime = DateTime.Now;
                existingCredential.LastAccessTime = DateTime.Now;
            }

            await SaveChangesAsync();
        }

        public async Task SaveConnectionStringAsync(ConnectionString connectionString)
        {
            var model = connectionString.Encrypt();

            var existingCredential = await ConnectionStrings.FirstOrDefaultAsync(s => s.Value == model.Value);

            if (existingCredential == null)
            {
                await ConnectionStrings.AddAsync(model);
            }
            else
            {
                existingCredential.Name = model.Name;
                existingCredential.LastAccessTime = DateTime.Now;
            }

            await SaveChangesAsync();
        }

        public async Task<List<DatabaseCredential>> GetDatabaseCredentialsAsync()
        {
            return await DatabaseCredentials
                .AsNoTracking()
                .Where(x => x.DatabaseType != DatabaseType.SQLite)
                .OrderByDescending(x => x.LastAccessTime)
                .Select(x => x.Decrypt())
                .ToListAsync();
        }

        public async Task<List<ConnectionString>> GetConnectionStringsAsync()
        {
            return await ConnectionStrings
                .AsNoTracking()
                .OrderByDescending(x => x.LastAccessTime)
                .Select(x => x.Decrypt())
                .ToListAsync();
        }

        public async Task RemoveDatabaseCredentialAsync(Guid id)
        {
            var credential = await DatabaseCredentials.FindAsync(id);
            if (credential != null)
            {
                DatabaseCredentials.Remove(credential);
                await SaveChangesAsync();
            }
        }

        public async Task RemoveConnectionStringAsync(Guid id)
        {
            var connectionString = await ConnectionStrings.FindAsync(id);
            if (connectionString != null)
            {
                ConnectionStrings.Remove(connectionString);
                await SaveChangesAsync();
            }
        }

        public async Task<bool> IsDatabaseCredentialOrConnectionStringExistsAsync()
        {
            return await DatabaseCredentials.AsNoTracking().AnyAsync() || await ConnectionStrings.AsNoTracking().AnyAsync();
        }
    }
}
