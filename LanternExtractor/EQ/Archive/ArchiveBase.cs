using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Archive
{
    public abstract class ArchiveBase
    {
        public string FilePath { get; }
        public string FileName { get; }
        protected List<ArchiveFile> _files = new List<ArchiveFile>();
        protected Dictionary<string, ArchiveFile> _fileNameReference = new Dictionary<string, ArchiveFile>();
        protected ILogger _logger;
        public bool IsWldArchive { get; set; }
        public Dictionary<string, string> FilenameChanges = new Dictionary<string, string>();

        protected ArchiveBase(string filePath, ILogger logger)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            _logger = logger;
        }

        public abstract bool Initialize();

        public ArchiveFile GetFile(string fileName)
        {
            return !_fileNameReference.ContainsKey(fileName) ? null : _fileNameReference[fileName];
        }

        public ArchiveFile GetFile(int index)
        {
            if (index < 0 || index >= _files.Count)
            {
                return null;
            }

            return _files[index];
        }

        public ArchiveFile[] GetAllFiles()
        {
            return _files.ToArray();
        }

        public void WriteAllFiles(string folder)
        {
            foreach (var file in _files)
            {
                FileWriter.WriteBytesToDisk(file.Bytes, folder, file.Name);
            }
        }

        public void RenameFile(string originalName, string newName)
        {
            if (!_fileNameReference.ContainsKey(originalName))
            {
                return;
            }

            var file = _fileNameReference[originalName];
            _fileNameReference.Remove(originalName);
            file.Name = newName;
            _fileNameReference[newName] = file;
        }
    }
}
