using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x32 - Vertex Color
    /// A list of colors, one per vertex, representing baked lighting data
    /// </summary>
    class VertexColor : WldFragment
    {
        /// <summary>
        /// The vertex colors corresponding with each vertex
        /// </summary>
        public List<Color> VertexColors { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int unknown = reader.ReadInt32();

            int vertexColorCount = reader.ReadInt32();

            // Typically contains 1
            int unknown2 = reader.ReadInt32();

            // Typically contains 200
            int unknown3 = reader.ReadInt32();

            // Typically contains 0
            int unknown4 = reader.ReadInt32();

            VertexColors = new List<Color>();

            for (int i = 0; i < vertexColorCount / 4; ++i)
            {
                VertexColors.Add(new Color
                {
                    R = reader.ReadInt32(),
                    G = reader.ReadInt32(),
                    B = reader.ReadInt32(),
                    A = reader.ReadInt32()
                });
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x32: Vertex color count: " + VertexColors.Count);
        }
    }
}