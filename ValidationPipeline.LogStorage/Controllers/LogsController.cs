using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.Controllers
{
    [Produces("application/json")]
    [ResponseCache(CacheProfileName = "Default")]
    [Route("api/[controller]")]
    [Authorize]
    public class LogsController : Controller
    {
        public const string MetaDataKeyPrefix = "file_";

        private readonly IArchiveService _archiveService;
        private readonly IStorageService _storageService;
        private readonly IOptionsSnapshot<CdnOptions> _cdnOptions;

        public LogsController(IArchiveService archiveService, IStorageService storageService,
            IOptionsSnapshot<CdnOptions> cdnOptions)
        {
            _archiveService = archiveService;
            _storageService = storageService;
            _cdnOptions = cdnOptions;
        }

        #region API Methods

        /// <summary>
        /// This method allows you to provide a new zip archive or update an existing one.
        /// </summary>
        /// <remarks>
        /// Contents of request body should be binary stream of the file to be uploaded.
        /// Content type is expected to be application/zip.
        /// This method only supports files up to 60MB in size.
        /// </remarks>
        /// <param name="archiveFileName">File name of the archive</param>
        /// <returns></returns>
        /// <response code="201">Returns data about inner files from the newly uploaded archive</response>
        /// <response code="400">If no content was provided</response>
        /// <response code="401">If request is unauthorized</response>
        /// <response code="415">If uploaded archive's content type is other than application/zip</response>
        [ResponseCache(CacheProfileName = "Never")]
        [HttpPut("{archiveFileName}")]
        [ProducesResponseType(typeof(IEnumerable<ArchiveResponse>), 201)]
        public async Task<IActionResult> UploadAsync(string archiveFileName)
        {
            if (Request.ContentLength == null || Request.ContentLength.Value == 0)
                return BadRequest();

            if (!IsContentTypeSupportedForUpload())
                return new UnsupportedMediaTypeResult();

            using (var stream = new MemoryStream())
            {
                // Request.Body can only move forward once
                Request.Body.CopyTo(stream);

                if (!_archiveService.Initialize(stream))
                    return new UnsupportedMediaTypeResult();

                if (_archiveService.IsEmpty())
                    return BadRequest($"{archiveFileName} is empty!");

                var metaData = _archiveService.GetMetaData();

                // Blob Storage Metadata Name only tolerates C# identifiers
                // That's why we create our own name before passing it to StorageService
                var metaDictionary = metaData.Select((value, index) => 
                    new { Key = MetaDataKeyPrefix + index, Value = value })
                    .ToDictionary(i => i.Key, i => i.Value);
                
                await _storageService.UploadAsync(archiveFileName, stream, metaDictionary);

                var archiveResponse = CreateArchiveResponse(archiveFileName, metaDictionary);
                return Created(Request.GetEncodedUrl(), archiveResponse);
            }
        }

        /// <summary>
        /// This method allows you to retrieve data about inner files of a stored archive.
        /// </summary>
        /// <param name="archiveFileName">File name of the archive</param>
        /// <returns></returns>
        /// <response code="200">Returns data about inner files from the specified archive file name</response>
        /// <response code="401">If request is unauthorized</response>
        /// <response code="404">If no archive was found for the specified file name</response>
        /// <response code="500">If specified archive is present but its metadata is corrupt</response>
        [HttpGet("{archiveFileName}")]
        [ProducesResponseType(typeof(IEnumerable<ArchiveResponse>), 200)]
        public async Task<IActionResult> GetMetaDataAsync(string archiveFileName)
        {
            var exists = await _storageService.ExistsAsync(archiveFileName);
            if (!exists)
                return NotFound($"{archiveFileName} was not found!");

            var metaDictionary = await _storageService.GetMetaDataAsync(archiveFileName);

            // Select only metadata stored by us
            metaDictionary = metaDictionary.Where(entry => entry.Key.StartsWith(MetaDataKeyPrefix))
                .ToDictionary(entry => entry.Key, entry => entry.Value);

            if (!metaDictionary.Any())
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);

            var archiveResponse = CreateArchiveResponse(archiveFileName, metaDictionary);

            return Ok(archiveResponse);
        }

        #endregion

        #region Helpers

        private bool IsContentTypeSupportedForUpload()
        {
            return !string.IsNullOrWhiteSpace(Request.ContentType) &&
                   Request.ContentType.Equals(CommonConstants.ZipContentType,
                       StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<ArchiveResponse> CreateArchiveResponse(string archiveFileName,
            IDictionary<string, MetaData> metaDictionary)
        {
            var protocol = Request.IsHttps ? "https" : "http";
            var host = $"{protocol}://{Request.Host.Value}";
            var edgeUrl = !string.IsNullOrWhiteSpace(_cdnOptions.Value.EdgeUrl)
                ? _cdnOptions.Value.EdgeUrl
                : host;

            return metaDictionary.Select(entry => new ArchiveResponse
            {
                Url = $"{edgeUrl}{CommonConstants.StaticFilesPath}/{archiveFileName}/{entry.Key}",
                Bytes = entry.Value.Length
            });
        }

        #endregion
    }
}
