using System.Collections.Generic;
using Serilog;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// VertexColorsReference (0x33)
    /// Internal name: None
    /// References a VertexColor fragment. Referenced by an ObjectInstance fragment.
    /// </summary>
    class VertexColorsReference : WldFragment
    {
        /// <summary>
        /// Reference to a vertex color
        /// </summary>
        public VertexColors VertexColors { get; private set; }

        public override void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat);
            Name = stringHash[-Reader.ReadInt32()];
            VertexColors = fragments[Reader.ReadInt32() - 1] as VertexColors;
        }

        public override void OutputInfo()
        {
            base.OutputInfo();

            if (VertexColors == null)
            {
                return;
            }

            Log.Information("-----");
            Log.Information("0x33: Vertex color reference: " + VertexColors.Index + 1);
        }
    }
}
