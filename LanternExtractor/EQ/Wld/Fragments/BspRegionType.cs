using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using GlmSharp;
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
        
        public string RegionString { get; set; }

        public enum ZonelineType
        {
            Reference,
            Absolute
        }
        
        public class ZonelineInfo
        {
            public ZonelineType Type;
            public int Index;
            public vec3 Position;
            public int Heading;
            public int ZoneIndex { get; set; }
        }

        public ZonelineInfo Zoneline;

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
                DecodeZoneline(regionTypeString);

                RegionString = regionTypeString;
            }
            else
            {
                //logger.LogError("Unknown region type: " + regionTypeString);
            }
        }

        private void DecodeZoneline(string regionTypeString)
        {
            Zoneline = new ZonelineInfo();
            
            // TODO: Verify this
            if (regionTypeString == "drntp_zone")
            {
                Zoneline.Type = ZonelineType.Reference;
                Zoneline.Index = 0;
                return;
            }
            
            int zoneId = Convert.ToInt32(regionTypeString.Substring(5, 5));

            if (zoneId == 255)
            {
                int zonelineId = Convert.ToInt32(regionTypeString.Substring(10, 6));

                Zoneline.Type = ZonelineType.Reference;
                Zoneline.Index = zonelineId;
                
                return;
            }

            Zoneline.ZoneIndex = zoneId;

            
            float x = GetValueFromRegionString(regionTypeString.Substring(10, 6));
            float y = GetValueFromRegionString(regionTypeString.Substring(16, 6));
            float z = GetValueFromRegionString(regionTypeString.Substring(22, 6));
            int rot = Convert.ToInt32(regionTypeString.Substring(28, 3));
            
            Zoneline.Type = ZonelineType.Absolute;
            Zoneline.Position = new vec3(x, y, z);
            Zoneline.Heading = rot;
        }

        private float GetValueFromRegionString(string substring)
        {
            if (substring.StartsWith("-"))
            {
                return -Convert.ToSingle(substring.Substring(1, 5));
            }
            else
            {
                return Convert.ToSingle(substring);
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
                if (RegionType == RegionType.Zoneline)
                {
                    
                }
                
                bspRegions[regionIndex].SetRegionFlag(this);
            }
        }
    }
}