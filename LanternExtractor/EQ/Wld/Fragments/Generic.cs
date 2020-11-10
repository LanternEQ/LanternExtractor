using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Generic Fragment (0xXX)
    /// Used for unknown fragments
    /// Left the functions here for debugging purposes
    /// </summary>
    class Generic : WldFragment
    {
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
        }
    }
}