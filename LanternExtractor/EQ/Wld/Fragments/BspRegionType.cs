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
    public class BspRegionType : WldFragment
    {
        /// <summary>
        /// The region type associated with the region list
        /// </summary>
        public RegionType RegionType { get; private set; }

        public List<int> BspRegionIndices { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();
            int regionCount = reader.ReadInt32();
        
            BspRegionIndices = new List<int>();

            for (int i = 0; i < regionCount; ++i)
            {
                BspRegionIndices.Add(reader.ReadInt32());
            }

            int regionStringSize = reader.ReadInt32();

            string regionTypeString = regionStringSize == 0 ? Name.ToLower() : 
                WldStringDecoder.DecodeString(reader.ReadBytes(regionStringSize)).ToLower();

            if(regionTypeString.StartsWith("wt"))
            {
                RegionType = RegionType.Water;
            }
            else if (regionTypeString.StartsWith("la"))
            {
                RegionType = RegionType.Lava;
            }
            else if (regionTypeString.StartsWith("drp"))
            {
                RegionType = RegionType.Pvp;
            }
            else if (regionTypeString.StartsWith("drn"))
            {
                RegionType = RegionType.Zoneline;
            }
            else
            {
                logger.LogError("Unknown region type: " + regionTypeString);
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x29: Region type: " + RegionType);
        }

        internal void LinkRegionType(List<BspRegion> bspRegions)
        {
            foreach(var regionIndex in BspRegionIndices)
            {
                bspRegions[regionIndex].SetRegionFlag(this);
            }
        }
    }
}