using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x03 - BitmapName
    /// This fragment contains the name of a bitmap image
    /// It's theoretically possible for this fragment to have more than one bitmap but it hasn't been seen
    /// </summary>
    class BitmapName : WldFragment
    {
        /// <summary>
        /// The bitmap names of this fragment - stored as a list because the client supports more than one
        /// </summary>
        public string Filename { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            // The client supports more than one bitmap reference but it doesn't look like it was ever used
            uint bitmapCount = reader.ReadUInt32();

            if (bitmapCount > 1)
            {
                logger.LogWarning("0x03: Bitmap count exceeds 1!");
            }

            ushort nameLength = reader.ReadUInt16();

            // Decode the bitmap name and trim the null character (c style strings)
            Filename = WldStringDecoder.DecodeString(reader.ReadBytes(nameLength)).ToLower();
            Filename = Filename.Substring(0, Filename.Length - 1);
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
            logger.LogInfo("0x03: Bitmap filename: " + Filename);
        }
    }
}