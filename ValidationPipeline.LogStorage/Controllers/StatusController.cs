using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class StatusController : Controller
    {
        private readonly IOptionsSnapshot<BlobStorageOptions> _options;

        public StatusController(IOptionsSnapshot<BlobStorageOptions> options)
        {
            _options = options;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(_options.Value);
        }
    }
}
