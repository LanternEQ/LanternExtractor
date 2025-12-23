using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Archive
{
    /// <summary>
    /// Loads and can extract files in the PFS archive
    /// </summary>
    public class PfsArchive : ArchiveBase
    {
        public PfsArchive(string filePath, ILogger logger) : base(filePath, logger)
        {
        }

        public override bool Initialize()
        {
            Logger.LogInfo("PfsArchive: Started initialization of archive: " + FileName);

            if (!File.Exists(FilePath))
            {
                Logger.LogError("PfsArchive: File does not exist at: " + FilePath);
                return false;
            }

            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                var reader = new BinaryReader(fileStream);
                int directoryOffset = reader.ReadInt32();
                var pfsMagic = reader.ReadUInt32();
                var pfsVersion = reader.ReadInt32();
                reader.BaseStream.Position = directoryOffset;

                int fileCount = reader.ReadInt32();
                var fileNames = new List<string>();

                for (int i = 0; i < fileCount; i++)
                {
                    uint crc = reader.ReadUInt32();
                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();

                    if (offset > reader.BaseStream.Length)
                    {
                        Logger.LogError("PfsArchive: Corrupted PFS length detected!");
                        return false;
                    }

                    long cachedOffset = reader.BaseStream.Position;
                    var fileBytes = new byte[size];

                    reader.BaseStream.Position = offset;

                    uint inflatedSize = 0;

                    while (inflatedSize != size)
                    {
                        uint deflatedLength = reader.ReadUInt32();
                        uint inflatedLength = reader.ReadUInt32();

                        if (deflatedLength >= reader.BaseStream.Length)
                        {
                            Logger.LogError("PfsArchive: Corrupted file length detected!");
                            return false;
                        }

                        byte[] compressedBytes = reader.ReadBytes((int)deflatedLength);
                        byte[] inflatedBytes;

                        if (!InflateBlock(compressedBytes, (int)inflatedLength, out inflatedBytes, Logger))
                        {
                            Logger.LogError("PfsArchive: Error occured inflating data");
                            return false;
                        }

                        inflatedBytes.CopyTo(fileBytes, inflatedSize);
                        inflatedSize += inflatedLength;
                    }

                    // EQZip saved archives use 0xFFFFFFFFU for filenames
                    // https://github.com/Shendare/EQZip/blob/b181ec7658ea9880984d58271cbab924ab8dd702/EQArchive.cs#L517
                    if (crc == 0x61580AC9 || (crc == 0xFFFFFFFFU && fileNames.Count == 0))
                    {
                        var dictionaryStream = new MemoryStream(fileBytes);
                        var dictionary = new BinaryReader(dictionaryStream);
                        uint filenameCount = dictionary.ReadUInt32();

                        for (uint j = 0; j < filenameCount; ++j)
                        {
                            uint fileNameLength = dictionary.ReadUInt32();
                            string filename = new string(dictionary.ReadChars((int)fileNameLength));
                            fileNames.Add(filename.Substring(0, filename.Length - 1));
                        }

                        reader.BaseStream.Position = cachedOffset;

                        continue;
                    }

                    Files.Add(new PfsFile(crc, size, offset, fileBytes));

                    reader.BaseStream.Position = cachedOffset;
                }

                // Sort files by offset so we can assign names
                Files.Sort((x, y) => x.Offset.CompareTo(y.Offset));

                // Assign file names
                for (int i = 0; i < Files.Count; ++i)
                {
                    switch(pfsVersion)
                    {
                        case 0x10000:
                            // PFS version 1 files do not appear to contain the filenames
                            if (Files[i] is PfsFile pfsFile)
                            {
                                pfsFile.Name = $"{pfsFile.Crc:X8}.bin";
                            }
                            break;
                        case 0x20000:
                            Files[i].Name = fileNames[i];
                            FileNameReference[fileNames[i]] = Files[i];

                            if (!IsWldArchive && fileNames[i].EndsWith(LanternStrings.WldFormatExtension))
                            {
                                IsWldArchive = true;
                            }
                            break;
                        default:
                            Logger.LogError("PfsArchive: Unexpected pfs version: " + FileName);
                            break;
                    }
                }

                Logger.LogInfo("PfsArchive: Finished initialization of archive: " + FileName);
            }

            return true;
        }

        private static bool InflateBlock(byte[] deflatedBytes, int inflatedSize, out byte[] inflatedBytes,
            ILogger logger)
        {
            var inputStream = new MemoryStream(deflatedBytes);
            var decompressStream = new ZLibStream(inputStream, CompressionMode.Decompress);
            var outputStream = new MemoryStream();

            try
            {
                decompressStream.CopyTo(outputStream);
                inflatedBytes = outputStream.ToArray();
                return true;
            }
            catch (Exception e)
            {
                inflatedBytes = null;
                logger.LogError("PfsArchive: Exception caught while inflating bytes: " + e);
                return false;
            }
        }
    }
}
