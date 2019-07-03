using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x29 - Region Flag
    /// Associates a list of regions with a specified region flag (Water, Lava, PvP or Zoneline)
    /// </summary>
    class RegionFlag : WldFragment
    {
        /// <summary>
        /// The list of regions that correspond with the region flag
        /// </summary>
        public List<BspRegion> Regions { get; private set; }

        /// <summary>
        /// The region type associated with the region list
        /// </summary>
        public RegionType RegionType { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            int regions = reader.ReadInt32();

            Regions = new List<BspRegion>();

            for (int i = 0; i < regions; ++i)
            {
                int reference = reader.ReadInt32();

                if (reference == 0)
                {
                    continue;
                }

                Regions.Add(fragments[reference - 1] as BspRegion);
            }

            int size2 = reader.ReadInt32();

            //TODO: read in and set region flag
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x29: Region type: " + RegionType);
            logger.LogInfo("0x29: Region count: " + Regions.Count);
        }
    }
}