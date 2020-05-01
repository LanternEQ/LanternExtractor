using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x11 - Skeleton Track Reference
    /// A reference to a skeleton track fragment (0x12)
    /// </summary>
    class SkeletonHierarchyReference : WldFragment
    {
        public SkeletonHierarchy SkeletonHierarchy { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            SkeletonHierarchy = fragments[reference - 1] as SkeletonHierarchy;

            int params1 = reader.ReadInt32();

            if (params1 != 0)
            {
                
            }
            
            // Confirmed end
            
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (SkeletonHierarchy != null)
            {
                logger.LogInfo("-----");
                logger.LogInfo("0x11: Skeleton track reference: " + SkeletonHierarchy.Index + 1);
            }
        }
    }
}