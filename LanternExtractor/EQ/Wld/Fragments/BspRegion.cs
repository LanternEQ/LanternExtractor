using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x22 - BSP Region
    /// BSP regions are leaf nodes in the BSP tree. They can contain references to mesh data (0x36)
    /// This fragment is largely unhandled as we don't need the tree data or the PVS (potentially visible set)
    /// </summary>
    public class BspRegion : WldFragment
    {
        /// <summary>
        /// Does this fragment contain geometry?
        /// </summary>
        public bool ContainsPolygons { get; private set; }

        /// <summary>
        /// A reference to the mesh data (0x36) for this fragment
        /// </summary>
        public Mesh RegionMesh { get; private set; }

        public BspRegionType Type { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            // Flags
            // 0x181 - Regions with polygons
            // 0x81 - Regions without
            // Bit 5 - PVS is WORDS
            // Bit 7 - PVS is bytes
            int flags = reader.ReadInt32();

            if (flags == 0x181)
            {
                ContainsPolygons = true;
            }

            int unknown1 = reader.ReadInt32();

            int data1Size = reader.ReadInt32();

            int data2Size = reader.ReadInt32();

            int unknown2 = reader.ReadInt32();

            int data3Size = reader.ReadInt32();

            int data4Size = reader.ReadInt32();

            int unknown3 = reader.ReadInt32();

            int data5Size = reader.ReadInt32();

            int data6Size = reader.ReadInt32();

            // Move past data1 and 2
            reader.BaseStream.Position += ((12 * data1Size) + (12 * data2Size));

            // Move past data3
            for (int i = 0; i < data3Size; ++i)
            {
                // Get the flags and size of the data 3 structure
                int data3Flags = reader.ReadInt32();
                int data3Size2 = reader.ReadInt32();

                reader.BaseStream.Position += (data3Size2 * 4);
            }

            // Move past the data 4
            for (int i = 0; i < data4Size; ++i)
            {
                // Unhandled for now
            }

            // Move past the data5
            for (int i = 0; i < data5Size; i++)
            {
                reader.BaseStream.Position += (7 * 4);
            }

            // Get the size of the PVS and allocate memory
            short pvsSize = reader.ReadInt16();

            reader.BaseStream.Position += pvsSize;

            // Move past the unknowns 
            uint bytes = reader.ReadUInt32();

            reader.BaseStream.Position += 16;

            // Get the mesh reference index and link to it
            if (ContainsPolygons)
            {
                int meshReference = reader.ReadInt32();
                RegionMesh = (Mesh) fragments[meshReference - 1];
            }
        }

        public void SetRegionFlag(BspRegionType bspRegionType)
        {
            Type = bspRegionType;
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x22: Contains polygons: " + ContainsPolygons);

            if (ContainsPolygons)
            {
                logger.LogInfo("0x22: Mesh index: " + RegionMesh.Index);
            }
        }
    }
}