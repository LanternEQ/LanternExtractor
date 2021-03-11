using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// MeshAnimatedVerticesReference (0x2F)
    /// Internal name: None
    /// References a MeshAnimatedVertices fragment.
    /// This fragment is referenced from the Mesh fragment, if it's animated.
    /// </summary>
    public class MeshAnimatedVerticesReference : WldFragment
    {
        public MeshAnimatedVertices MeshAnimatedVertices { get; set; }
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            MeshAnimatedVertices = fragments[Reader.ReadInt32() -1] as MeshAnimatedVertices;
            int flags = Reader.ReadInt32();
        }
        
        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
        }
    }
}