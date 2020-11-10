using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// BitmapName (0x03)
    /// Internal Name: ?
    /// This fragment contains the name of a bitmap image. It supports more than one bitmap but this is never used
    /// Fragment end is padded to end on a DWORD boundary
    /// </summary>
    public class BitmapName : WldFragment
    {
        /// <summary>
        /// The filename of the referenced bitmap
        /// </summary>
        public string Filename { get; set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));
            Name = stringHash[-reader.ReadInt32()];

            // The client supports more than one bitmap reference but is never used
            int bitmapCount = reader.ReadInt32();

            if (bitmapCount > 1)
            {
                logger.LogWarning("BitmapName: Bitmap count exceeds 1.");
            }

            int nameLength = reader.ReadInt16();

            // Decode the bitmap name and trim the null character (c style strings)
            byte[] nameBytes = reader.ReadBytes(nameLength);
            Filename = WldStringDecoder.DecodeString(nameBytes);
            Filename = Filename.ToLower().Substring(0, Filename.Length - 1);
        }

        public string GetExportFilename()
        {
            if(Filename.EndsWith(".dds"))
            {
                return Filename;
            }
            else
            {
                return GetFilenameWithoutExtension() + ".png";
            }
        }

        public string GetFilenameWithoutExtension()
        {
            return Filename.Substring(0, Filename.Length - 4);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("BitmapName: Filename: " + Filename);
        }
    }
}