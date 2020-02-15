using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x13 - Skeleton Piece Track Reference
    /// Refers to the skeleton piece fragment (0x12)
    /// </summary>
    public class TrackFragment : WldFragment
    {
        /// <summary>
        /// Reference to a skeleton piece
        /// </summary>
        public TrackDefFragment TrackDefFragment { get; set; }
        
        public bool IsProcessed { get; set; }
        
        // TODO: Determine what this does
        public int SleepMs { get; set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];
            
            int reference = reader.ReadInt32();
            
            int flags = reader.ReadInt32();

            BitAnalyzer bitAnalyzer = new BitAnalyzer(flags);

            if (bitAnalyzer.IsBitSet(0))
            {
                SleepMs = reader.ReadInt32();
            }
            else
            {
                SleepMs = 0;
            }
            
            TrackDefFragment = fragments[reference - 1] as TrackDefFragment;
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (TrackDefFragment != null)
            {
                logger.LogInfo("-----");
                logger.LogInfo("0x13: Skeleton piece reference: " + TrackDefFragment.Index + 1);
            }
        }
    }
}