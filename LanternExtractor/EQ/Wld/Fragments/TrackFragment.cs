using System.Collections.Generic;
using System.IO;
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

        public void SetTrackData(string modelName, string animationName, string pieceName)
        {
            ModelName = modelName;
            AnimationName = animationName;
            PieceName = pieceName;
        }

        /// <summary>
        /// This is only ever called when we are finding additional animations.
        /// All animations that are not the default skeleton animations:
        /// 1. Start with a 3 letter animation abbreviation (e.g. C05)
        /// 2. Continue with a 3 letter model name
        /// 3. Continue with the skeleton piece name
        /// 4. End with _TRACK
        /// </summary>
        public void ParseTrackData()
        {
            string cleanedName = FragmentNameCleaner.CleanName(this, true);

            if (cleanedName.Length < 6)
            {
                ModelName = cleanedName;
                return;
            }
            
            AnimationName = cleanedName.Substring(0, 3);
            cleanedName = cleanedName.Remove(0, 3);
            ModelName = cleanedName.Substring(0, 3);
            cleanedName = cleanedName.Remove(0, 3);
            PieceName = cleanedName;
        }
    }
}