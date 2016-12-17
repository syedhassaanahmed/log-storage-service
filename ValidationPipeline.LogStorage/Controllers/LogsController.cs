using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ValidationPipeline.LogStorage.Extensions;
using ValidationPipeline.LogStorage.FileProviders;
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

            //Convert filename to valid path for static file download
            archiveFileName = archiveFileName.Replace(".", "_");
            var metaData = _archiveService.GetMetaData(Request.Body).ToList();

            // Blob Storage Metadata Name only tolerates C# identifiers
            // That's why we base64 encode file names before passing it to StorageService
            Base64Encode(metaData);
            await _storageService.UploadAsync(archiveFileName, Request.Body, metaData);

            var archiveResponse = CreateArchiveResponse(archiveFileName, metaData);
            return Created(Request.GetEncodedUrl(), archiveResponse);
        }

        [HttpGet("{archiveFileName}")]
        public async Task<IActionResult> GetMetaDataAsync(string archiveFileName)
        {
            //Convert filename to valid path for static file download
            archiveFileName = archiveFileName.Replace(".", "_");

            var exists = await _storageService.ExistsAsync(archiveFileName);
            if (!exists)
                return NotFound();

            var metaData = await _storageService.GetMetaDataAsync(archiveFileName);
            var archiveResponse = CreateArchiveResponse(archiveFileName, metaData);

            return Ok(archiveResponse);
        }

        [HttpGet("{archiveFileName}/{innerFileName}")]
        public async Task<IActionResult> DownloadAsync(string archiveFileName, string innerFileName)
        {
            var exists = await _storageService.InnerFileExistsAsync(archiveFileName, innerFileName);
            if (!exists)
                return NotFound();

            var archiveStream = await _storageService.DownloadAsync(archiveFileName);

            var decodedFileName = innerFileName.FromBase64();
            var innerFileStream = _archiveService.ExtractInnerFile(archiveStream, decodedFileName);

            return new FileStreamResult(innerFileStream, MediaTypeHeaderValue.Parse(BinaryContentType))
            {
                FileDownloadName = decodedFileName
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

        private IEnumerable<ArchiveResponse> CreateArchiveResponse(string archiveFileName, 
            IEnumerable<LogStorageFileInfo> metaData)
        {
            var requestUri = new Uri(Request.GetEncodedUrl());
            var baseUri = requestUri.OriginalString.Replace(requestUri.PathAndQuery, string.Empty);

            return metaData.Select(fileInfo => new ArchiveResponse
            {
                Url = $"{baseUri}{Startup.StaticFilesPath}/{archiveFileName}/{fileInfo.Name}"
            });
        }

        private static void Base64Encode(IEnumerable<LogStorageFileInfo> metaData)
        {
            foreach (var fileInfo in metaData)
                fileInfo.Name = fileInfo.Name.ToBase64();
        }

        #endregion
    }
}
