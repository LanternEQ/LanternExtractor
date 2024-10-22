using System.Collections.Generic;
using Serilog;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// LightSourceReference (0x1C)
    /// Internal name: None
    /// References a LightSource fragment.
    /// </summary>
    class LightSourceReference : WldFragment
    {
        /// <summary>
        /// The light source (0x1B) fragment reference
        /// </summary>
        public LightSource LightSource { get; private set; }

        public override void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat);
            Name = stringHash[-Reader.ReadInt32()];
            LightSource = fragments[Reader.ReadInt32() - 1] as LightSource;
        }

        public override void OutputInfo()
        {
            base.OutputInfo();
            Log.Information("-----");
            Log.Information("LightSourceReference: Reference: " + (LightSource.Index + 1));
        }
    }
}
