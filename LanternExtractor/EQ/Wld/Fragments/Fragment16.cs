using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x16 - Zone Unknown
    /// An unknown fragment
    /// </summary>
    class Fragment16 : WldFragment
    {
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            // Should be 0.1
            float unknown = reader.ReadSingle();
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
        }
    }
}