using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Ambient Light (0x2A)
    /// Defines the ambient light for a group of regions. This fragment exists, but is unused in the Trilogy client. 
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

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
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
            logger.LogInfo("AmbientLight: Reference: " + (LightReference.Index + 1));
            logger.LogInfo("AmbientLight: Regions: " + Regions.Count);
        }
    }
}