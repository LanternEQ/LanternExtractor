using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x1C - Light Source Reference
    /// References a LightSource (0x1B) fragment
    /// </summary>
    class LightSourceReference : WldFragment
    {
        /// <summary>
        /// The light source (0x1B) fragment reference
        /// </summary>
        public LightSource LightSource { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            LightSource = fragments[reference - 1] as LightSource;
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x1C: Reference: " + (LightSource.Index + 1));
        }
    }
}