using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x2D - Mesh Reference
    /// Contains a reference to a mesh fragment (0x36)
    /// </summary>
    public class MeshReference : WldFragment
    {
        /// <summary>
        /// The mesh reference
        /// </summary>
        public Mesh Mesh { get; private set; }
        
        public AlternateMesh AlternateMesh { get; private set; }
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            int reference = Reader.ReadInt32() - 1;
            
            Mesh = fragments[reference] as Mesh;

            if (Mesh != null)
            {
                return;
            }
            
            AlternateMesh = fragments[reference] as AlternateMesh;

            if (AlternateMesh != null)
            {
                return;
            }
            
            logger.LogError("MeshReference: NO MESH: " + reference);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (Mesh != null)
            {
                logger.LogInfo("-----");
                logger.LogInfo("0x2D: Mesh reference: " + Mesh.Index);
            }
        }
    }
}