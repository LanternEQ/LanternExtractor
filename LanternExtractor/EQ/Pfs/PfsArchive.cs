using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zlib;
using LanternExtractor.EQ.Wld;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Pfs
{
    /// <summary>
    /// Loads and can extract files in the PFS archive
    /// </summary>
    public class PfsArchive
    {
        /// <summary>
        /// The OS path to the file
        /// </summary>
        private readonly string _filePath;

        /// <summary>
        /// The OS path to which the files will be extracted
        /// </summary>
        private readonly string _exportPath;

        /// <summary>
        /// The name of the PFS file we have loaded
        /// </summary>
        private readonly string _fileName;

        /// <summary>
        /// A list containing all files
        /// </summary>
        private readonly List<PfsFile> _files = new List<PfsFile>();

        /// <summary>
        /// A dictionary of all files allowing direct name access
        /// </summary>
        private readonly Dictionary<string, PfsFile> _fileNameReference = new Dictionary<string, PfsFile>();

        /// <summary>
        /// The logger for debug output
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor that initializes the file path and logger
        /// </summary>
        /// <param name="filePath">The OS path to the file</param>
        /// <param name="logger">The logger for debug output</param>
        public PfsArchive(string filePath, ILogger logger)
        {
            _filePath = filePath;
            _fileName = Path.GetFileName(filePath);
            _logger = logger;

            if (filePath.EndsWith("_obj.s3d") || filePath.EndsWith("_chr.s3d"))
            {
                _exportPath = _filePath.Substring(0, filePath.Length - 8);
            }
            else
            {
                _exportPath = _filePath;
            }
        }

        /// <summary>
        /// Initializes the archive from the disk
        /// </summary>
        /// <returns>Whether or not the PFS archive load was successful</returns>
        public bool Initialize()
        {
            _logger.LogInfo("Started initialization of archive: " + _fileName);

            if (!File.Exists(_filePath))
            {
                _logger.LogError("File does not exist at: " + _filePath);
                return false;
            }

            var reader = new BinaryReader(File.Open(_filePath, FileMode.Open));

            int directoryOffset = reader.ReadInt32();

            reader.BaseStream.Position = directoryOffset;

            int fileCount = reader.ReadInt32();

            var fileNames = new List<string>();

            for (int i = 0; i < fileCount; i++)
            {
                uint crc = reader.ReadUInt32();
                uint offset = reader.ReadUInt32();
                uint size = reader.ReadUInt32();

                // Check for corrupt S3Ds
                if (offset > reader.BaseStream.Length)
                {
                    _logger.LogError("Corrupted file detected!");
                    return false;
                }

                var cachedOffset = reader.BaseStream.Position;

                var fileBytes = new byte[size];

                reader.BaseStream.Position = offset;

                uint inflatedSize = 0;

                while (inflatedSize != size)
                {
                    uint deflatedLength = reader.ReadUInt32();
                    uint inflatedLength = reader.ReadUInt32();

                    // Sometimes there can be incorrect values 
                    if (deflatedLength >= reader.BaseStream.Length)
                    {
                        _logger.LogError("Corrupted file detected!");
                        return false;
                    }
                    
                    byte[] compressedBytes = reader.ReadBytes((int) deflatedLength);

                    byte[] inflatedBytes;

                    if (!InflateBlock(compressedBytes, (int) inflatedLength, out inflatedBytes, _logger))
                    {
                        _logger.LogError("An error occured while inflating data");
                        return false;
                    }

                    inflatedBytes.CopyTo(fileBytes, inflatedSize);

                    //reader.BaseStream.Position += deflatedLength;
                    inflatedSize += inflatedLength;
                }

                if (crc == 0x61580AC9)
                {
                    var dictionaryStream = new MemoryStream(fileBytes);
                    var dictionary = new BinaryReader(dictionaryStream);
                    uint filenameCount = dictionary.ReadUInt32();

                    for (uint j = 0; j < filenameCount; ++j)
                    {
                        uint fileNameLength = dictionary.ReadUInt32();
                        string filename = new string(dictionary.ReadChars((int) fileNameLength));
                        fileNames.Add(filename.Substring(0, filename.Length - 1));
                    }

                    reader.BaseStream.Position = cachedOffset;

                    continue;
                }

                _files.Add(new PfsFile(crc, size, offset, fileBytes));

                reader.BaseStream.Position = cachedOffset;
            }

            // Sort by offset
            _files.Sort((x, y) => x.Offset.CompareTo(y.Offset));

            for (int i = 0; i < _files.Count; ++i)
            {
                _files[i].SetFileName(fileNames[i]);
                _fileNameReference.Add(fileNames[i], _files[i]);
            }

            _logger.LogInfo("Finished initialization of archive: " + _fileName);

            return true;
        }

        /// <summary>
        /// Inflates (decompressed) a single block of data
        /// </summary>
        /// <param name="deflatedBytes">The deflated (compressed) data bytes</param>
        /// <param name="inflatedSize">The size of the bytes once inflated</param>
        /// <param name="inflatedBytes">The inflated (decompressed) data bytes</param>
        /// <param name="logger">The logger for debug output</param>
        /// <returns>Whether or not the inflation was successful</returns>
        private static bool InflateBlock(byte[] deflatedBytes, int inflatedSize, out byte[] inflatedBytes,
            ILogger logger)
        {
            var output = new byte[inflatedSize];

            using (var memoryStream = new MemoryStream())
            {
                var zlibCodec = new ZlibCodec();
                zlibCodec.InitializeInflate(true);

                zlibCodec.InputBuffer = deflatedBytes;
                zlibCodec.AvailableBytesIn = deflatedBytes.Length;
                zlibCodec.NextIn = 0;
                zlibCodec.OutputBuffer = output;

                foreach (FlushType f in new[] {FlushType.None, FlushType.Finish})
                {
                    int bytesToWrite;

                    do
                    {
                        zlibCodec.AvailableBytesOut = inflatedSize;
                        zlibCodec.NextOut = 0;
                        try
                        {
                            zlibCodec.Inflate(f);
                        }
                        catch (Exception e)
                        {
                            inflatedBytes = null;
                            logger.LogError("Exception caught while inflating bytes: " + e);
                            return false;
                        }

                        bytesToWrite = inflatedSize - zlibCodec.AvailableBytesOut;
                        if (bytesToWrite > 0)
                            memoryStream.Write(output, 0, bytesToWrite);
                    } while (f == FlushType.None &&
                             (zlibCodec.AvailableBytesIn != 0 || zlibCodec.AvailableBytesOut == 0) ||
                             f == FlushType.Finish && bytesToWrite != 0);
                }

                zlibCodec.EndInflate();


                inflatedBytes = output;
                return true;
            }
        }

        /// <summary>
        /// Writes all files to disk
        /// </summary>
        /// <param name="folderName">An optional subfolder to </param>
        public void WriteAllFiles(string subfolderName = "")
        {
            for (int i = 0; i < _files.Count; ++i)
            {
                WriteFile(i, subfolderName);
            }
        }

        /// <summary>
        /// Writes all files using the texture types information.
        /// This allows us to add the correct filename prefix for the different shader types.
        /// </summary>
        /// <param name="textureTypes">The types (shader) of all textures</param>
        /// <param name="folderName">An optional folder name to put the files into</param>
        /// <param name="onlyTextures">Are we exporting only textures?</param>
        public void WriteAllFiles(Dictionary<string, List<ShaderType>> textureTypes, string folderName = "",
            bool onlyTextures = false)
        {
            if (textureTypes == null)
            {
                return;
            }
            
            for (int i = 0; i < _files.Count; ++i)
            {
                string filename = _files[i].Name;

                if (filename.EndsWith(".bmp"))
                {
                    if (!textureTypes.ContainsKey(filename))
                    {
                        continue;
                    }

                    foreach (ShaderType type in textureTypes[filename])
                    {
                        WriteImage(i, type, folderName);
                    }
                }
                else if(filename.EndsWith(".dds"))
                {
                    WriteFile(i, folderName);
                }
                else
                {
                    if (onlyTextures)
                    {
                        continue;
                    }

                    WriteFile(i, folderName);
                }
            }
        }

        /// <summary>
        /// Writes a file based on the index in the file list
        /// </summary>
        /// <param name="index">The index of the file to write</param>
        /// <param name="subfolderName">An optional folder name to put the files into</param>
        private void WriteFile(int index, string subfolderName = "")
        {
            if (index < 0 || _files.Count <= index || _files[index].Bytes == null)
            {
                return;
            }

            string directoryPath = Path.GetFileNameWithoutExtension(_exportPath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }

            if (!string.IsNullOrEmpty(subfolderName))
            {
                directoryPath += "/" + subfolderName;
            }

            Directory.CreateDirectory(directoryPath);

            var binaryWriter =
                new BinaryWriter(
                    File.OpenWrite(directoryPath + "/" + _files[index].Name));
            binaryWriter.Write(_files[index].Bytes);
            binaryWriter.Close();
        }

        /// <summary>
        /// Writes a specific file by name
        /// </summary>
        /// <param name="index"></param>
        /// <param name="fileName">The name of the file to be written</param>
        /// <param name="folderName">An optional folder name to put the files into</param>
        public void WriteFile(int index, string fileName, string folderName = "")
        {
            for (var i = 0; i < _files.Count; i++)
            {
                PfsFile file = _files[i];

                if (file.Name != fileName)
                {
                    continue;
                }

                WriteFile(i, folderName);
                break;
            }
        }

        /// <summary>
        /// Writes an image to disk taking the shader type into account
        /// </summary>
        /// <param name="index">The index in the file list</param>
        /// <param name="type">The shader type</param>
        /// <param name="folderName">An optional folder name to put the files into</param>
        private void WriteImage(int index, ShaderType type, string folderName = "")
        {
            if (index < 0 || index >= _files.Count)
            {
                return;
            }

            if (!_files[index].Name.EndsWith(".bmp"))
            {
                return;
            }

            if (_files[index].Bytes == null)
            {
                return;
            }

            string pngName = _files[index].Name.Substring(0, _files[index].Name.Length - 3) + "png";
            string exportPath = Path.GetFileNameWithoutExtension(_exportPath) + "/";

            if (!string.IsNullOrEmpty(folderName))
            {
                exportPath += folderName;
            }

            var byteStream = new MemoryStream(_files[index].Bytes);
            ImageWriter.WriteImage(byteStream, exportPath, pngName, type, _logger);
        }

        /// <summary>
        /// Returns a reference to a PFS file in the archive.
        /// </summary>
        /// <param name="fileName">The name of the file requested</param>
        /// <returns>A reference to the file, or null if it was not found</returns>
        public PfsFile GetFile(string fileName)
        {
            return !_fileNameReference.ContainsKey(fileName) ? null : _fileNameReference[fileName];
        }
    }
}