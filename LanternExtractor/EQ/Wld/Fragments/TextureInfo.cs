using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x04 - Texture Info
    /// This fragment contains a reference to a 0x03 fragment and information about animation
    /// </summary>
    class TextureInfo : WldFragment
    {
        /// <summary>
        /// Is the texture animated?
        /// </summary>
        public bool IsAnimated { get; private set; }

        /// <summary>
        /// The bitmap names (0x03) referenced
        /// </summary>
        public List<BitmapName> BitmapNames { get; private set; }

        /// <summary>
        /// The number of milliseconds before the next texture is swapped in (animation)
        /// </summary>
        public int AnimationDelayMs { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            BitmapNames = new List<BitmapName>();

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            var bitAnalyzer = new BitAnalyzer(flags);

            bool params3 = bitAnalyzer.IsBitSet(3);

            int size1 = reader.ReadInt32();

            if (params3)
            {
                IsAnimated = true;
                AnimationDelayMs = reader.ReadInt32();
            }

            for (int i = 0; i < size1; ++i)
            {
                int bitmapIndex = reader.ReadInt32();

                BitmapNames.Add(fragments[bitmapIndex - 1] as BitmapName);
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x04: Animated: " + IsAnimated);

            if (IsAnimated)
            {
                logger.LogInfo("0x04: Animation delay: " + AnimationDelayMs + "ms");
            }

            string references = string.Empty;

            for (var i = 0; i < BitmapNames.Count; i++)
            {
                if (i != 0)
                {
                    references += ", ";
                }

                BitmapName bitmapName = BitmapNames[i];
                references += (bitmapName.Index + 1);
            }

            logger.LogInfo("0x04: Reference(s): " + references);
        }
    }
}