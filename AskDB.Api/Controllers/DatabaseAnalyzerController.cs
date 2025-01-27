using AskDB.Api.Constants;
using DatabaseAnalyzer;
using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using Helper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace AskDB.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseAnalyzerController : ControllerBase
    {
        [HttpGet("DatabaseTypes")]
        [ResponseCache(Duration = CachingTime.Forever, Location = ResponseCacheLocation.Any, NoStore = true)]
        public IActionResult GetDatabaseTypes()
        {
            var types = Enum
                .GetValues(typeof(DatabaseType))
                .Cast<DatabaseType>()
                .Select(t => new
                {
                    Id = t,
                    DatabaseType = t.ToString(),
                    SampleConnectionString = Extractor.GetEnumDescription(t)
                })
                .ToList();

            return Ok(types);
        }

        [HttpPost("InitConnection")]
        public async Task<IActionResult> Init(DatabaseType databaseType, string connectionStringBox)
        {
            Analyzer.DbExtractor = databaseType switch
            {
                DatabaseType.SqlServer => new SqlServerExtractor(connectionStringBox.Trim()),
                DatabaseType.PostgreSQL => new PostgreSqlExtractor(connectionStringBox.Trim()),
                DatabaseType.SQLite => new SqliteExtractor(connectionStringBox.Trim()),
                DatabaseType.MySQL => new MySqlExtractor(connectionStringBox.Trim()),
                _ => throw new NotSupportedException("Not Supported"),
            };

            try
            {
                await Analyzer.DbExtractor.ExtractTables();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            Analyzer.DbExtractor.Tables = Analyzer.DbExtractor.Tables
                .Where(t => t.Columns.Count > 0)
                .OrderBy(t => t.Name)
                .ToList();

            return Ok(Analyzer.DbExtractor.Tables);
        }

        [HttpPost("ExtractTableSqlSchemas")]
        public ActionResult<List<string>> ExtractTableSqlSchemas([FromBody] List<string> tableNames)
        {
            var schemas = Analyzer.DbExtractor.Tables
                 .Where(t => tableNames.Contains(t.Name))
                 .Select(d => new
                 {
                     Name = d.Name,
                     SqlSchema = d.ToString()
                 })
                 .ToList();

            return Ok(schemas);
        }

        [HttpPost("SuggestedQueries")]
        public async Task<ActionResult<List<string>>> GetSuggestedQueries([FromBody] List<string> tableNames, sbyte maxDataSamples = 15, sbyte maxQueries = 100)
        {
            Analyzer.SelectedTables = Analyzer.DbExtractor.Tables
                .Where(t => tableNames.Contains(t.Name))
                .ToList();

            await Analyzer.ExtractSampleData(maxDataSamples);

            var tasks = new List<Task<List<string>>>();

            sbyte partitions = 4;
            sbyte queriesPerPartition = (sbyte)(maxQueries / partitions);

            for (int i = 0; i < partitions; i++)
            {
                bool isTrue = i < partitions / 2;
                tasks.Add(Analyzer.GetSuggestedQueries(isTrue, queriesPerPartition));
            }

            var results = await Task.WhenAll(tasks);

            return Ok(results.SelectMany(r => r).ToList());
        }

        [HttpPost("ExportResultsAsCsv")]
        public async Task<IActionResult> ExportResultsAsCsv(string sqlCommand)
        {
            try
            {
                var table = await Analyzer.DbExtractor.Execute(sqlCommand);
                var sb = new StringBuilder();

                var totalColumns = table.Columns.Count;

                for (int i = 0; i < totalColumns; i++)
                {
                    sb.Append(StringTool.EscapeCsvValue(table.Columns[i].ColumnName));
                    if (i < totalColumns - 1)
                    {
                        sb.Append(',');
                    }
                }

                foreach (DataRow row in table.Rows)
                {
                    for (int i = 0; i < totalColumns; i++)
                    {
                        var value = row[i] != null ? StringTool.EscapeCsvValue(row[i].ToString()) : string.Empty;
                        sb.Append(value);
                        if (i < totalColumns - 1)
                        {
                            sb.Append(',');
                        }
                    }
                }

                var byteArray = Encoding.UTF8.GetBytes(sb.ToString());
                var stream = new MemoryStream(byteArray);

                return File(stream, "text/csv", $"{DateTime.Now.Ticks}-AskDB.csv");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetInsight")]
        public async Task<ActionResult<string>> GetInsight(string sqlCommand)
        {
            try
            {
                var dataTable = await Analyzer.DbExtractor.Execute(sqlCommand);
                var insight = await Analyzer.GetQuickInsight(sqlCommand, dataTable);
                return Ok(insight);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ExecuteSqlCommand")]
        public async Task<IActionResult> ExecuteSqlCommand([FromBody] List<string> tableNames, string sqlCommand)
        {
            try
            {
                var table = new DataTable();
                var commander = new SqlCommander();

                try
                {
                    table = await Analyzer.DbExtractor.Execute(sqlCommand);
                }
                catch
                {
                    Analyzer.SelectedTables = Analyzer.DbExtractor.Tables.Where(t => tableNames.Contains(t.Name)).ToList();
                    commander = await Analyzer.GetSql(sqlCommand);
                    table = await Analyzer.DbExtractor.Execute(commander.Output);
                }

                return Ok(new
                {
                    Table = table,
                    SqlQuery = commander.Output
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}