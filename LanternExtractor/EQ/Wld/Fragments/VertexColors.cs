using System;
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
    class VertexColors : WldFragment
    {
        /// <summary>
        /// The vertex colors corresponding with each vertex
        /// </summary>
        public List<Color> Colors { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
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

            Colors = new List<Color>();

            for (int i = 0; i < vertexColorCount / 4; ++i)
            {
                int color = reader.ReadInt32();
                
                byte[] colorBytes = BitConverter.GetBytes(color);
                int r = colorBytes[0];
                int g = colorBytes[1];
                int b = colorBytes[2];
                int a = colorBytes[3];
                
                Colors.Add(new Color{R = r, G = g, B = b, A = a});
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x32: Vertex color count: " + Colors.Count);
        }
    }
}