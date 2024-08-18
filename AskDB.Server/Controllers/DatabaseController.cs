using DatabaseAnalyzer;
using DatabaseAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace AskDB.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(ILogger<DatabaseController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetSchemaAsObject")]
        public async Task<ActionResult<List<Table>>> GetSchemaAsObject(DatabaseType databaseType, string connectionString)
        {
            try
            {
                var tables = await Analyzer.GetDatabaseSchemas(databaseType, connectionString);
                return Ok(tables);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetSchemaAsString")]
        public async Task<ActionResult<string>> GetSchemaAsString(DatabaseType databaseType, string connectionString)
        {
            try
            {
                var tables = await Analyzer.GetDatabaseSchemas(databaseType, connectionString);
                var schemas = string.Join(Environment.NewLine, tables.AsParallel().Select(table => table.ToString())).Trim();

                return Ok(schemas);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
