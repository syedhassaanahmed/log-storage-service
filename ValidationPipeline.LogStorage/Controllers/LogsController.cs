using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage.Controllers
{
    [Route("api/[controller]")]
    public class LogsController : Controller
    {
        public const string MetaDataKeyPrefix = "file_";

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
            if (Request.ContentLength == null || Request.ContentLength.Value == 0)
                return BadRequest();

            if (!IsContentTypeSupportedForUpload())
                return new UnsupportedMediaTypeResult();

            using (var stream = new MemoryStream())
            {
                // Request.Body can only move forward once
                Request.Body.CopyTo(stream);

                if (!_archiveService.IsValid(stream))
                    return new UnsupportedMediaTypeResult();

                if (_archiveService.IsEmpty(stream))
                    return BadRequest($"{archiveFileName} is empty!");

                var metaData = _archiveService.GetMetaData(stream).ToList();

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
            var requestUri = new Uri(Request.GetEncodedUrl());
            var baseUri = requestUri.OriginalString.Replace(requestUri.PathAndQuery, string.Empty);

            return metaDictionary.Select(entry => new ArchiveResponse
            {
                Url = $"{baseUri}{CommonConstants.StaticFilesPath}/{archiveFileName}/{entry.Key}",
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
