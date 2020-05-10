﻿using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Bitmap Info (0x04)
    /// Internal name: SPRITE
    /// This fragment contains a reference to a 0x03 fragment and information about animation
    /// </summary>
    public class BitmapInfo : WldFragment
    {
        /// <summary>
        /// Is the texture animated?
        /// </summary>
        public bool IsAnimated { get; private set; }

        /// <summary>
        /// The bitmap names referenced. 
        /// </summary>
        public List<BitmapName> BitmapNames { get; private set; }

        /// <summary>
        /// The number of milliseconds before the next texture is swapped.
        /// </summary>
        public int AnimationDelayMs { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            BitmapNames = new List<BitmapName>();

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            var bitAnalyzer = new BitAnalyzer(flags);

            IsAnimated = bitAnalyzer.IsBitSet(3);

            int bitmapCount = reader.ReadInt32();

            if (IsAnimated)
            {
                AnimationDelayMs = reader.ReadInt32();
            }

            for (int i = 0; i < bitmapCount; ++i)
            {
                int fragmentIndex = reader.ReadInt32() - 1;
                BitmapNames.Add(fragments[fragmentIndex] as BitmapName);
            }
            
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
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