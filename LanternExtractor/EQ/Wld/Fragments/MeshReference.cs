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

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            Mesh = fragments[reference - 1] as Mesh;

            if (Mesh == null)
            {
                logger.LogError("Null mesh reference");
                return;
            }
            
            if (Mesh.Name.ToLower().Contains("templife"))
            {
                
            }

            int something = reader.ReadInt32();

            if (something != 0)
            {
                
            }
            
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
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