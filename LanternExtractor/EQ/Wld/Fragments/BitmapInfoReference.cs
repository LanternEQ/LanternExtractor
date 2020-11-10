using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// BitmapInfoReference (0x05)
    /// Internal name: ?
    /// Contains a reference to a BitmapInfo fragment
    /// </summary>
    public class BitmapInfoReference : WldFragment
    {
        /// <summary>
        /// The reference to the BitmapInfo
        /// </summary>
        public BitmapInfo BitmapInfo { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int fragmentReference = reader.ReadInt32() - 1;

            BitmapInfo = fragments[fragmentReference] as BitmapInfo;

            // Either 0 or 80 - unknown
            int flags = reader.ReadInt32();
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("BitmapInfoReference: Reference: " + (BitmapInfo.Index + 1));
        }
    }
}