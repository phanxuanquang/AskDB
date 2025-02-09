using Gemini.NET;
using Helper;
using Microsoft.AspNetCore.Mvc;

namespace AskDB.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CachingController : ControllerBase
    {
        public CachingController()
        {
            Cache.Init().Wait();
        }

        [HttpGet("SearchFromCachedData")]
        public ActionResult<IEnumerable<string>> Search(string keyword, bool includeConnectionKey)
        {
            var results = includeConnectionKey
                    ? Cache.Get(k => k.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) && Validator.CanBeValidApiKey(k))
                    : Cache.Get(k => k.StartsWith(keyword, StringComparison.OrdinalIgnoreCase) && !Validator.CanBeValidApiKey(k));
            return Ok(results);
        }
    }
}
