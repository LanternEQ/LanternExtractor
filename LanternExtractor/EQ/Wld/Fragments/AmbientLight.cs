using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x2A - Ambient Light
    /// Defines the ambient light for a group of regions. I have never actually seen this fragment used. 
    /// </summary>
    class AmbientLight : WldFragment
    {
        /// <summary>
        /// A reference to a 0x13 light source reference fragment which defines the light for the regions
        /// </summary>
        public LightSourceReference LightReference { get; private set; }

        /// <summary>
        /// The regions that the light reference apply to
        /// </summary>
        public List<int> Regions { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            LightReference = fragments[reference - 1] as LightSourceReference;

            int flags = reader.ReadInt32();

            int regionCount = reader.ReadInt32();

            Regions = new List<int>();

            for (int i = 0; i < regionCount; ++i)
            {
                int regionId = reader.ReadInt32();
                Regions.Add(regionId);
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x2A: Reference: " + (LightReference.Index + 1));
            logger.LogInfo("0x2A: Regions: " + Regions.Count);
        }
    }
}