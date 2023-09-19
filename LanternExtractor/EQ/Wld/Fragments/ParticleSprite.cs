﻿using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// ParticleSprite (0x26)
    /// Internal name: _SPB
    /// Assumed to be a particle sprite fragment.
    /// </summary>
    public class ParticleSprite : WldFragment
    {
        public BitmapInfoReference BitmapInfoReference { get; private set; }

        public override void Initialize(int index, int size, byte[] data, List<WldFragment> fragments, Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            int value_04 = Reader.ReadInt32(); // flags? always 0
            int fragmentRef = Reader.ReadInt32();
            int value_12 = Reader.ReadInt32(); // always the same value. unlikely a float, or bytes. Not color.
            BitmapInfoReference = fragments[fragmentRef - 1] as BitmapInfoReference;
        }
    }
}
