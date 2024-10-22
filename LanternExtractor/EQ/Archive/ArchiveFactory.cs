using System;
using System.IO;

namespace LanternExtractor.EQ.Archive
{
    public class ArchiveFactory
    {
        private const uint T3dMagic = 0xffff3d02;
        private const uint PfsMagic = 0x20534650;

        public static ArchiveBase GetArchive(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // Skip detection and let the archive Initialize fail.
                return new NullArchive(filePath);
            }

            var archiveType = GetArchiveTypeFromMagic(filePath);
            if (archiveType == ArchiveType.Unknown)
            {
                archiveType = GetArchiveTypeFromFilename(filePath);
            }

            switch (archiveType)
            {
                case ArchiveType.Pfs:
                    return new PfsArchive(filePath);
                case ArchiveType.T3d:
                    return new T3dArchive(filePath);
                default:
                    throw new ArgumentException("Unknown archive type", "filePath");
            }
        }

        private static ArchiveType GetArchiveTypeFromMagic(string filePath)
        {
            uint data;
            using (BinaryReader br = new BinaryReader(File.OpenRead(filePath)))
            {
                data = br.ReadUInt32();
                if (data == T3dMagic)
                {
                    return ArchiveType.T3d;
                }

                data = br.ReadUInt32();
                if (data == PfsMagic)
                {
                    return ArchiveType.Pfs;
                }
            }

            return ArchiveType.Unknown;
        }

        private static ArchiveType GetArchiveTypeFromFilename(string filePath)
        {
            string archiveExt = Path.GetExtension(filePath)?.ToLower();

            switch (archiveExt)
            {
                case LanternStrings.T3dFormatExtension:
                    return ArchiveType.T3d;
                case LanternStrings.S3dFormatExtension:
                case LanternStrings.PfsFormatExtension:
                case LanternStrings.PakFormatExtension:
                    return ArchiveType.Pfs;
            }

            return ArchiveType.Unknown;
        }
    }
}
