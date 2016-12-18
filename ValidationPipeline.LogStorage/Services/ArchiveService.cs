using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Services
{
    public class ArchiveService : IArchiveService, IDisposable
    {
        private ZipArchive _zipArchive;

        public bool Initialize(Stream archiveStream)
        {
            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            archiveStream.Position = 0;

            try
            {
                _zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true);
                return true;
            }
            catch (InvalidDataException e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }

        public bool IsEmpty()
        {
            if (_zipArchive == null)
                throw new ArgumentException("Archive Service was not initialized!");

            return _zipArchive.Entries.Count == 0;
        }

        public IEnumerable<MetaData> GetMetaData()
        {
            if (_zipArchive == null)
                throw new ArgumentException("Archive Service was not initialized!");

            return _zipArchive.Entries.Select(entry => new MetaData
            {
                Name = entry.Name,
                Length = entry.Length,
                LastModified = entry.LastWriteTime
            });
        }

        public Stream ExtractInnerFile(string innerFileName)
        {
            if (_zipArchive == null)
                throw new ArgumentException("Archive Service was not initialized!");

            if (string.IsNullOrWhiteSpace(innerFileName))
                throw new ArgumentNullException(nameof(innerFileName));

            return _zipArchive.GetEntry(innerFileName)?.Open();
        }

        public void Dispose()
        {
            _zipArchive?.Dispose();
        }
    }
}
