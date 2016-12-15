using Microsoft.AspNetCore.Mvc;

namespace ValidationPipeline.LogStorage.Controllers
{
    [Route("api/[controller]")]
    public class LogsController : Controller
    {
        [HttpPost]
        public IActionResult Upload()
        {
            return Ok();
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return Ok();
        }
    }
}
