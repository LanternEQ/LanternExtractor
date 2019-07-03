using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x37 - Mesh Animated Vertices
    /// Contains a list of frames each containing a vertex position the model
    /// The frame vertices are cycled through animating the model
    /// </summary>
    class MeshAnimatedVertices : WldFragment
    {
        /// <summary>
        /// The model frames
        /// </summary>
        public List<List<vec3>> Frames { get; private set; }

        /// <summary>
        /// The delay between the vertex swaps
        /// </summary>
        public int Delay { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            Frames = new List<List<vec3>>();

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            int vertexCount = reader.ReadInt16();

            int frameCount = reader.ReadInt16();

            Delay = reader.ReadInt16();
            int param2 = reader.ReadInt16();

            float scale = 1.0f / (1 << reader.ReadInt16());

            for (int i = 0; i < frameCount; ++i)
            {
                var positions = new List<vec3>();

                for (int j = 0; j < vertexCount; ++j)
                {
                    float x = reader.ReadInt16() * scale;
                    float y = reader.ReadInt16() * scale;
                    float z = reader.ReadInt16() * scale;

                    positions.Add(new vec3(x, y, z));
                }

                Frames.Add(positions);
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x37: Frame count: " + Frames.Count);
        }
    }
}