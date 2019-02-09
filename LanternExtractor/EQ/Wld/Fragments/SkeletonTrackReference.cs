using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x11 - Skeleton Track Reference
    /// A reference to a skeleton track fragment (0x12)
    /// </summary>
    class SkeletonTrackReference : WldFragment
    {
        public SkeletonTrack SkeletonTrack { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            SkeletonTrack = fragments[reference - 1] as SkeletonTrack;

            //Console.WriteLine("0x11: " + Name);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (SkeletonTrack != null)
            {
                logger.LogInfo("-----");
                logger.LogInfo("0x11: Skeleton track reference: " + SkeletonTrack.Index + 1);
            }
        }
    }
}