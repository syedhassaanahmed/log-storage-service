using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.Controllers
{
    [Route("api/[controller]")]
    public class LogsController : Controller
    {
        private const string ZipContentType = "application/zip";

        private readonly IArchiveService _archiveService;

        private bool HasBody => Request.Body.Length > 0;
        private bool IsContentTypeSupportedForUpload => !string.IsNullOrWhiteSpace(Request.ContentType) &&
                                                        Request.ContentType.Equals(ZipContentType,
                                                            StringComparison.OrdinalIgnoreCase);

        public LogsController(IArchiveService archiveService)
        {
            _archiveService = archiveService;
        }
        
        [HttpPut("{zipFileName}.zip")]
        public IActionResult Upload(string zipFileName)
        {
            if (!HasBody)
                return BadRequest();

            if (!IsContentTypeSupportedForUpload)
                return new UnsupportedMediaTypeResult();

            if (!_archiveService.IsValid(Request.Body))
                return new UnsupportedMediaTypeResult();

            if (!_archiveService.IsEmpty(Request.Body))
                return BadRequest();

            var fileNames = _archiveService.GetFileNames(Request.Body);
            var filesInfo = fileNames.Select(file => new LogFileInfo {Url = file});

            return Created(string.Empty, filesInfo);
        }

        [HttpGet("{zipFileName}/{fileName}")]
        public IActionResult GetFile(string zipFileName, string fileName)
        {
            return Ok();
        }
    }
}
