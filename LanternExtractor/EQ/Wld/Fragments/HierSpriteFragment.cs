using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x11 - Skeleton Track Reference
    /// A reference to a skeleton track fragment (0x12)
    /// </summary>
    class HierSpriteFragment : WldFragment
    {
        public HierSpriteDefFragment HierSpriteDefFragment { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            HierSpriteDefFragment = fragments[reference - 1] as HierSpriteDefFragment;

            //Console.WriteLine("0x11: " + Name);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (HierSpriteDefFragment != null)
            {
                logger.LogInfo("-----");
                logger.LogInfo("0x11: Skeleton track reference: " + HierSpriteDefFragment.Index + 1);
            }
        }
    }
}