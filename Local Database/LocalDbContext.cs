using Local_Database.Models;
using Local_Database.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data.SQLite;

namespace Local_Database
{
    public class LocalDbContext(DbContextOptions<LocalDbContext> options) : DbContext(options)
    {
        public DbSet<DatabaseCredential> DatabaseCredential { get; set; }
        public DbSet<QueryHistory> QueryHistory { get; set; }
        public DbSet<GoogleApiKey> GoogleApiKey { get; set; }
        public DbSet<SystemInstruction> SystemInstruction { get; set; }

        public static async Task SetupLocalDatabaseAsync(string databasePath, string scriptUrl)
        {
            try
            {
                if (File.Exists(databasePath))
                {
                    return;
                }

                SQLiteConnection.CreateFile(databasePath);

                using var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
                await connection.OpenAsync();

                using (var pragmaCommand = new SQLiteCommand("PRAGMA synchronous = OFF; PRAGMA journal_mode = MEMORY;", connection))
                {
                    await pragmaCommand.ExecuteNonQueryAsync();
                }

                using HttpClient client = new();
                using Stream stream = await client.GetStreamAsync(scriptUrl);
                using StreamReader reader = new(stream);
                var sqlScript = await reader.ReadToEndAsync();

                using var command = new SQLiteCommand(sqlScript, connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while setting up SQLite database for local caching.", ex);
            }
        }

        public async Task<DatabaseCredential> GetDatabaseCredentialTemplateAsync(DatabaseType databaseType)
        {
            return await DatabaseCredential.FirstAsync(x => x.DatabaseType == databaseType);
        }

        public async Task<List<DatabaseCredential>> GetDatabaseCredentialsAsync(string keyword, DatabaseType databaseType, int count = 10)
        {
            return await DatabaseCredential
                .AsNoTracking()
                .Where(dbCredential => dbCredential.DatabaseType == databaseType
                    && (dbCredential.Database.Contains(keyword, StringComparison.OrdinalIgnoreCase) || dbCredential.Server.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.LastUpdatedTime)
                .Take(count)
                .ToListAsync();
        }

        public async Task<GoogleApiKey?> GetLatestGoogleApiKeyAsync()
        {
            return await GoogleApiKey.LastOrDefaultAsync(k => k.IsActive);
        }

        public async Task<SystemInstruction> GetSystemInstructionAsync(InstructionPurpose purpose)
        {
            return await SystemInstruction.FindAsync(purpose);
        }

        public async Task<List<QueryHistory>> GetLatestQueryHistoryAsync(int count = 10)
        {
            return await QueryHistory
                .AsNoTracking()
                .OrderByDescending(x => x.TotalExecutions)
                .Take(count)
                .ToListAsync();
        }
    }
}
