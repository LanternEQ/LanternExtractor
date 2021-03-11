using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// BspRegion (0x22)
    /// Internal Name: None
    /// Leaf nodes in the BSP tree. Can contain references to Mesh fragments.
    /// This fragment's PVS (potentially visible set) data is unhandled.
    /// </summary>
    public class BspRegion : WldFragment
    {
        /// <summary>
        /// Does this fragment contain geometry?
        /// </summary>
        public bool ContainsPolygons { get; private set; }

        /// <summary>
        /// A reference to the mesh fragment
        /// </summary>
        public Mesh Mesh { get; private set; }

        public BspRegionType RegionType { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];

            // Flags
            // 0x181 - Regions with polygons
            // 0x81 - Regions without
            // Bit 5 - PVS is WORDS
            // Bit 7 - PVS is bytes
            int flags = Reader.ReadInt32();

            if (flags == 0x181)
            {
                ContainsPolygons = true;
            }

            // Always 0
            int unknown1 = Reader.ReadInt32();
            int data1Size = Reader.ReadInt32();
            int data2Size = Reader.ReadInt32();

            // Always 0
            int unknown2 = Reader.ReadInt32();
            int data3Size = Reader.ReadInt32();
            int data4Size = Reader.ReadInt32();

            // Always 0
            int unknown3 = Reader.ReadInt32();
            int data5Size = Reader.ReadInt32();
            int data6Size = Reader.ReadInt32();

            // Move past data1 and 2
            Reader.BaseStream.Position += 12 * data1Size + 12 * data2Size;

            // Move past data3
            for (int i = 0; i < data3Size; ++i)
            {
                int data3Flags = Reader.ReadInt32();
                int data3Size2 = Reader.ReadInt32();
                Reader.BaseStream.Position += data3Size2 * 4;
            }

            // Move past the data 4
            for (int i = 0; i < data4Size; ++i)
            {
                // Unhandled for now
            }

            // Move past the data5
            for (int i = 0; i < data5Size; i++)
            {
                Reader.BaseStream.Position += 7 * 4;
            }

            // Get the size of the PVS and allocate memory
            short pvsSize = Reader.ReadInt16();
            Reader.BaseStream.Position += pvsSize;

            // Move past the unknowns 
            uint bytes = Reader.ReadUInt32();
            Reader.BaseStream.Position += 16;

            // Get the mesh reference index and link to it
            if (ContainsPolygons)
            {
                int meshReference = Reader.ReadInt32() - 1;
                Mesh = fragments[meshReference] as Mesh;
            }
        }

        public void SetRegionFlag(BspRegionType bspRegionType)
        {
            RegionType = bspRegionType;
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("BspRegion: Contains polygons: " + ContainsPolygons);

            if (ContainsPolygons)
            {
                logger.LogInfo("BspRegion: Mesh index: " + Mesh.Index);
            }
        }
    }
}