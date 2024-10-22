using System.Collections.Generic;
using Serilog;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// MeshReference (0x2D)
    /// Internal name: None
    /// Contains a reference to either a Mesh and LegacyMesh fragment.
    /// This fragment is referenced from a Skeleton fragment.
    /// </summary>
    public class MeshReference : WldFragment
    {
        public Mesh Mesh { get; private set; }

        public LegacyMesh LegacyMesh { get; private set; }

        public override void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat);
            Name = stringHash[-Reader.ReadInt32()];
            int reference = Reader.ReadInt32() - 1;
            Mesh = fragments[reference] as Mesh;

            if (Mesh != null)
            {
                return;
            }

            LegacyMesh = fragments[reference] as LegacyMesh;

            if (LegacyMesh != null)
            {
                return;
            }

            Log.Error("No mesh reference found for id: " + reference);
        }

        public override void OutputInfo()
        {
            base.OutputInfo();

            if (Mesh != null)
            {
                Log.Information("-----");
                Log.Information("0x2D: Mesh reference: " + Mesh.Index);
            }
        }
    }
}
