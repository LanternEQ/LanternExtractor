using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0xXX - Generic Fragment
    /// Left the functions here for debugging purposes
    /// </summary>
    class Generic : WldFragment
    {
        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
        }
    }
}