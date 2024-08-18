using Microsoft.AspNetCore.Mvc;

namespace AskDB.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenAIController : ControllerBase
    {
        private readonly ILogger<GenAIController> _logger;

        public GenAIController(ILogger<GenAIController> logger)
        {
            _logger = logger;
        }
    }
}
