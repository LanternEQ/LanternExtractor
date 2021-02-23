using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public class ParticleSprite : WldFragment
    {
        private BitmapInfoReference _bitmapReference;
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data, List<WldFragment> fragments, Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            int value_04 = Reader.ReadInt32(); // flags? always 0
            int fragmentRef = Reader.ReadInt32();
            int value_12 = Reader.ReadInt32(); // always the same value. unlikely a float, or bytes. Not color.
            _bitmapReference = fragments[fragmentRef - 1] as BitmapInfoReference;
        }
    }
}