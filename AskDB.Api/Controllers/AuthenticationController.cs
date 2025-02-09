using AskDB.Api.Constants;
using Gemini.NET;
using Helper;
using Microsoft.AspNetCore.Mvc;
using Octokit;

namespace AskDB.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        [HttpGet("LatestRelease")]
        [ResponseCache(Duration = CachingTime.HalfDay, Location = ResponseCacheLocation.Any, NoStore = true)]
        public async Task<ActionResult<Release>> GetLatestRelease()
        {
            var release = await Extractor.GetGithubLatestReleaseInfo();

            if (release == null)
            {
                return NotFound("Error while fetching the release information");
            }

            return Ok(release);
        }

        [HttpPost("ValidateGeminiApiKey")]
        [ResponseCache(Duration = CachingTime.HalfHour, Location = ResponseCacheLocation.Any, NoStore = true)]
        public async Task<IActionResult> ValidateGeminiApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest("API key is required.");
            }

            if (!Validator.CanBeValidApiKey(apiKey))
            {
                return Unauthorized("Invalid API key.");
            }

            var generator = new Generator(apiKey);
            var isValidApiKey = await generator.IsValidApiKeyAsync();

            if (!isValidApiKey)
            {
                return Unauthorized("Invalid API key.");
            }

            await Cache.Set(apiKey);
            Cache.ApiKey = apiKey;

            return Ok();
        }

        [HttpPost("Healthcheck")]
        public IActionResult CheckServerRunning()
        {
            return Ok("Server is running.");
        }
    }
}
