using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public class Fragment07 : WldFragment
    {
        private Fragment06 frag06;
        public override void Initialize(int index, FragmentType id, int size, byte[] data, List<WldFragment> fragments, Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            int fragRef = Reader.ReadInt32();
            frag06 = fragments[fragRef - 1] as Fragment06;
            int value_08 = Reader.ReadInt32(); // always 0
        }
    }
}