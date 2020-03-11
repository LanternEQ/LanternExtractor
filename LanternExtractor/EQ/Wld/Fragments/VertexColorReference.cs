using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x33 - Vertex Color Reference
    /// Contains a reference to a vertex color fragment (0x32)
    /// </summary>
    class VertexColorReference : WldFragment
    {
        /// <summary>
        /// Reference to a vertex color
        /// </summary>
        public VertexColors VertexColors { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            VertexColors = fragments[reference - 1] as VertexColors;
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (VertexColors == null)
            {
                return;
            }

            logger.LogInfo("-----");
            logger.LogInfo("0x33: Vertex color reference: " + VertexColors.Index + 1);
        }
    }
}