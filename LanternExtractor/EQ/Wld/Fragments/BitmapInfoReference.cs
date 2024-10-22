using System.Collections.Generic;
using Serilog;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// BitmapInfoReference (0x05)
    /// Internal name: None
    /// Contains a reference to a BitmapInfo fragment.
    /// </summary>
    public class BitmapInfoReference : WldFragment
    {
        /// <summary>
        /// The reference to the BitmapInfo
        /// </summary>
        public BitmapInfo BitmapInfo { get; private set; }

        public override void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat);
            Name = stringHash[-Reader.ReadInt32()];
            BitmapInfo = fragments[Reader.ReadInt32() - 1] as BitmapInfo;

            // Either 0 or 80 - unknown
            int flags = Reader.ReadInt32();
        }

        public override void OutputInfo()
        {
            base.OutputInfo();
            Log.Information("-----");
            Log.Information("BitmapInfoReference: Reference: " + (BitmapInfo.Index + 1));
        }
    }
}
