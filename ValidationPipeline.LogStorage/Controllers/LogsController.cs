using System;
using Microsoft.AspNetCore.Mvc;

namespace ValidationPipeline.LogStorage.Controllers
{
    [Route("api/[controller]")]
    public class LogsController : Controller
    {
        private const string ZipContentType = "application/zip";

        private bool IsContentTypeSupportedForUpload => !string.IsNullOrWhiteSpace(Request.ContentType) &&
                                                        Request.ContentType.Equals(ZipContentType,
                                                            StringComparison.OrdinalIgnoreCase);


        [HttpPost("{zipFileName}.zip")]
        public IActionResult Upload(string zipFileName)
        {
            if (Request.Body.Length == 0)
                return BadRequest();

            if (!IsContentTypeSupportedForUpload)
                return new UnsupportedMediaTypeResult();

            return Ok();
        }

        [HttpGet("{zipFileName}/{fileName}")]
        public IActionResult Get(string zipFileName, string fileName)
        {
            return Ok();
        }
    }
}
