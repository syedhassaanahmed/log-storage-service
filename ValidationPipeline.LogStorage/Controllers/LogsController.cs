using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.Controllers
{
    [Route("api/[controller]")]
    public class LogsController : Controller
    {
        private const string ZipContentType = "application/zip";
        private const string BinaryContentType = "application/octet-stream";

        private readonly IArchiveService _archiveService;
        private readonly IStorageService _storageService;

        public LogsController(IArchiveService archiveService, 
            IStorageService storageService)
        {
            _archiveService = archiveService;
            _storageService = storageService;
        }

        #region API Methods

        [HttpPut("{archiveFileName}")]
        public async Task<IActionResult> UploadAsync(string archiveFileName)
        {
            if (Request.Body.Length == 0)
                return BadRequest();

            if (!IsContentTypeSupportedForUpload())
                return new UnsupportedMediaTypeResult();

            if (!_archiveService.IsValid(Request.Body))
                return new UnsupportedMediaTypeResult();

            if (!_archiveService.IsEmpty(Request.Body))
                return BadRequest();

            var innerFileNames = _archiveService.GetInnerFileNames(Request.Body).ToList();
            await _storageService.UploadAsync(archiveFileName, Request.Body, innerFileNames);

            var filesInfo = CreateLogFilesInfo(innerFileNames);
            return Created(Request.GetEncodedUrl(), filesInfo);
        }

        [HttpGet("{archiveFileName}")]
        public async Task<IActionResult> GetInnerFilesInfoAsync(string archiveFileName)
        {
            var exists = await _storageService.ExistsAsync(archiveFileName);
            if (!exists)
                return NotFound();

            var innerFileNames = await _storageService.GetInnerFileNamesAsync(archiveFileName);
            var filesInfo = CreateLogFilesInfo(innerFileNames);

            return Ok(filesInfo);
        }

        [HttpGet("{archiveFileName}/{innerFileName}")]
        public async Task<IActionResult> DownloadAsync(string archiveFileName, string innerFileName)
        {
            var exists = await _storageService.ExistsAsync(archiveFileName);
            if (!exists)
                return NotFound();

            var archiveStream = await _storageService.DownloadAsync(archiveFileName);
            var innerFileStream = _archiveService.ExtractInnerFile(archiveStream, innerFileName);

            return new FileStreamResult(innerFileStream, MediaTypeHeaderValue.Parse(BinaryContentType))
            {
                FileDownloadName = innerFileName
            };
        }

        #endregion

        #region Helpers

        private bool IsContentTypeSupportedForUpload()
        {
            return !string.IsNullOrWhiteSpace(Request.ContentType) &&
                   Request.ContentType.Equals(ZipContentType,
                       StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<LogFileInfo> CreateLogFilesInfo(IEnumerable<string> innerFileNames)
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
