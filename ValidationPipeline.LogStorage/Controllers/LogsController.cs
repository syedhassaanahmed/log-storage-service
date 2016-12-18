using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.Controllers
{
    [ResponseCache(CacheProfileName = "Default")]
    [Route("api/[controller]")]
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

        [ResponseCache(CacheProfileName = "Never")]
        [HttpPut("{archiveFileName}")]
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

                var metaData = _archiveService.GetMetaData().ToList();

                // Blob Storage Metadata Name only tolerates C# identifiers
                // That's why we create our own name before passing it to StorageService
                var metaDictionary = CreateMetaDictionary(metaData);
                await _storageService.UploadAsync(archiveFileName, stream, metaDictionary);

                var archiveResponse = CreateArchiveResponse(archiveFileName, metaDictionary);
                return Created(Request.GetEncodedUrl(), archiveResponse);
            }
        }

        [HttpGet("{archiveFileName}")]
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
            return metaDictionary.Select(entry => new ArchiveResponse
            {
                Url = $"{_cdnOptions.Value.EdgeUrl}{CommonConstants.StaticFilesPath}/{archiveFileName}/{entry.Key}",
                Bytes = entry.Value.Length
            });
        }

        private static IDictionary<string, MetaData> CreateMetaDictionary(IList<MetaData> metaData)
        {
            var metaDictionary = new Dictionary<string, MetaData>();

            for (var i = 0; i < metaData.Count; i++)
            {
                metaDictionary.Add(MetaDataKeyPrefix + i, metaData[i]);
            }

            return metaDictionary;
        }

        #endregion
    }
}
