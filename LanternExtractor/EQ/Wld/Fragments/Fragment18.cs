using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Fragment18 (0x18) - PolygonAnimationReference?
    /// Internal Name: None
    /// Need to figure this fragment out.
    /// </summary>
    public class Fragment18 : WldFragment
    {
        public Fragment17 Fragment17;
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            Fragment17 = fragments[Reader.ReadInt32() - 1] as Fragment17;
            float params1 = Reader.ReadSingle();
        }
        
        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
        }
    }
}