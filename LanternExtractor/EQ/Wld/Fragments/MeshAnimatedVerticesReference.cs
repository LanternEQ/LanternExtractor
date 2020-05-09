using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public class MeshAnimatedVerticesReference : WldFragment
    {
        public MeshAnimatedVertices MeshAnimatedVertices { get; set; }
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();
            
            MeshAnimatedVertices = fragments[reference -1] as MeshAnimatedVertices;

            int flags = reader.ReadInt32();

            if (flags != 0)
            {
                
            }

            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
        }
        
        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
        }
    }
}