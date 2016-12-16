using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

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

        public IEnumerable<string> GetInnerFileNames(Stream archiveStream)
        {
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.Entries.Select(entry => entry.Name);
            }
        }

        public Stream ExtractFile(Stream archiveStream, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.GetEntry(fileName)?.Open();
            }
        }
    }
}
