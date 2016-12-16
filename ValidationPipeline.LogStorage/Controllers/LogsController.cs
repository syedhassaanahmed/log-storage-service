using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http.Extensions;
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

        public LogsController(IArchiveService archiveService)
        {
            _archiveService = archiveService;
        }

        #region API Methods

        [HttpPut("{archiveFileName}")]
        public IActionResult Upload(string archiveFileName)
        {
            if (Request.Body.Length == 0)
                return BadRequest();

            if (!IsContentTypeSupportedForUpload())
                return new UnsupportedMediaTypeResult();

            if (!_archiveService.IsValid(Request.Body))
                return new UnsupportedMediaTypeResult();

            if (!_archiveService.IsEmpty(Request.Body))
                return BadRequest();

            var innerFileNames = _archiveService.GetInnerFileNames(Request.Body);
            var filesInfo = CreateLogFileInfo(innerFileNames);

            return Created(Request.GetEncodedUrl(), filesInfo);
        }

        [HttpGet("{archiveFileName}")]
        public IActionResult GetInnerFilesInfo(string archiveFileName, string innerFileName)
        {
            return Ok();
        }

        [HttpGet("{archiveFileName}/{innerFileName}")]
        public IActionResult GetFile(string archiveFileName, string innerFileName)
        {
            return Ok();
        }

        #endregion

        #region Helpers

        private bool IsContentTypeSupportedForUpload()
        {
            return !string.IsNullOrWhiteSpace(Request.ContentType) &&
                   Request.ContentType.Equals(ZipContentType,
                       StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<LogFileInfo> CreateLogFileInfo(IEnumerable<string> innerFileNames)
        {
            var encodedUrl = Request.GetEncodedUrl();

            return innerFileNames.Select(file => new LogFileInfo
            {
                Url = $"{encodedUrl}/{file}"
            });
        }

        #endregion
    }
}
