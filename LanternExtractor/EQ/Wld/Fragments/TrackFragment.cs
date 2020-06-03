using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.EQ.Wld.Helpers;
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
        
        public int FrameMs { get; set; }
        
        public string ModelName;
        public string AnimationName;
        public string PieceName;

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];
            
            string cleanedName = FragmentNameCleaner.CleanName(this, false);
            string nameBase = cleanedName.Substring(0, 3);
            cleanedName = cleanedName.Remove(0, 3);
            
            if (!nameBase.Any(char.IsNumber))
            {
                AnimationName = "POS";
                ModelName = nameBase;
                PieceName = cleanedName == string.Empty ? "ROOT" : cleanedName;
            }
            else
            {
                AnimationName = nameBase;
                ModelName = cleanedName.Substring(0, 3);
                cleanedName = cleanedName.Remove(0, 3);
                PieceName = cleanedName;
            }
            
            int reference = reader.ReadInt32();
            
            TrackDefFragment = fragments[reference - 1] as TrackDefFragment;

            if (TrackDefFragment == null)
            {
                logger.LogError("Bad track def reference'");
            }

            
            // Either 4 or 5 - maybe something to look into
            // Bits are set 0, or 2. 0 has the extra field for delay.
            // 2 doesn't have any additional fields.
            int flags = reader.ReadInt32();

            BitAnalyzer bitAnalyzer = new BitAnalyzer(flags);

            if (bitAnalyzer.IsBitSet(0))
            {
                FrameMs = reader.ReadInt32();
            }
            else
            {
                FrameMs = 0;
            }
            
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
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