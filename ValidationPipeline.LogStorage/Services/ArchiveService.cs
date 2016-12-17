using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ValidationPipeline.LogStorage.Models;

namespace ValidationPipeline.LogStorage.Services
{
    public class ArchiveService : IArchiveService
    {
        public bool IsValid(Stream archiveStream)
        {
            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            archiveStream.Position = 0;

            try
            {
                using (new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
                {
                    return true;
                }
            }
            catch (InvalidDataException e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }

        public bool IsEmpty(Stream archiveStream)
        {
            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            archiveStream.Position = 0;

            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.Entries.Count == 0;
            }
        }

        public IEnumerable<MetaData> GetMetaData(Stream archiveStream)
        {
            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            archiveStream.Position = 0;

            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.Entries.Select(entry => new MetaData
                {
                    Name = entry.Name,
                    Length = entry.Length,
                    LastModified = entry.LastWriteTime
                });
            }
        }

        public Stream ExtractInnerFile(Stream archiveStream, string innerFileName)
        {
            if (archiveStream == null || archiveStream == Stream.Null)
                throw new ArgumentNullException(nameof(archiveStream));

            if (string.IsNullOrWhiteSpace(innerFileName))
                throw new ArgumentNullException(nameof(innerFileName));

            archiveStream.Position = 0;

            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true))
            {
                return archive.GetEntry(innerFileName)?.Open();
            }
        }
    }
}
