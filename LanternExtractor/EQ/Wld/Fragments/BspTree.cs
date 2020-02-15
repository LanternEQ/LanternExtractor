using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x21 - BSP Tree
    /// The BSP tree is a binary tree with each leaf node containing a region (0x22) and usually polygons
    /// BSP trees are generally not efficient especially when used in non BSP engines so we handle it but don't export it
    /// </summary>
    class BspTree : WldFragment
    {
        /// <summary>
        /// The BSP nodes contained within the tree
        /// </summary>
        public List<BspNode> Nodes { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int nodeCount = reader.ReadInt32();

            Nodes = new List<BspNode>();

            for (int i = 0; i < nodeCount; ++i)
            {
                var node = new BspNode()
                {
                    NormalX = reader.ReadSingle(),
                    NormalY = reader.ReadSingle(),
                    NormalZ = reader.ReadSingle(),
                    SplitDistance = reader.ReadSingle(),
                    RegionId = reader.ReadInt32(),
                    LeftNode = reader.ReadInt32() - 1,
                    RightNode = reader.ReadInt32() - 1
                };

                Nodes.Add(node);
            }
        }

        public void LinkBspRegions(List<WldFragment> fragments)
        {
            foreach (var node in Nodes)
            {
                if (node.RegionId == 0)
                {
                    continue;
                }

                node.Region = fragments[node.RegionId] as BspRegion;
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x21: Node count: " + Nodes.Count);
        }
    }
}