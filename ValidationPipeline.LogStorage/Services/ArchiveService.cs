using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ValidationPipeline.LogStorage.FileProviders;

namespace ValidationPipeline.LogStorage.Services
{
    public class ArchiveService : IArchiveService
    {
        public bool IsValid(Stream archiveStream)
        {
            try
            {
                using (new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
                {
                    return true;
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        public bool IsEmpty(Stream archiveStream)
        {
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.Entries.Count == 0;
            }
        }

        public IEnumerable<LogStorageFileInfo> GetMetaData(Stream archiveStream)
        {
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.Entries.Select(entry => new LogStorageFileInfo
                {
                    Length = entry.Length,
                    Name = entry.Name,
                    LastModified = entry.LastWriteTime
                });
            }
        }

        public Stream ExtractInnerFile(Stream archiveStream, string innerFileName)
        {
            if (string.IsNullOrWhiteSpace(innerFileName))
                throw new ArgumentNullException(nameof(innerFileName));

            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.GetEntry(innerFileName)?.Open();
            }
        }
    }
}
