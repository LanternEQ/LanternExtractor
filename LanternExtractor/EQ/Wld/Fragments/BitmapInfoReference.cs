using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x05 - TextureInfoReference
    /// This fragment contains a reference to a TextureInfo (0x04) fragment
    /// </summary>
    public class BitmapInfoReference : WldFragment
    {
        /// <summary>
        /// The reference to the texture info (0x04)
        /// </summary>
        public BitmapInfo BitmapInfo { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            BitmapInfo = fragments[reference - 1] as BitmapInfo;
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x05: Reference: " + (BitmapInfo.Index + 1));
        }
    }
}