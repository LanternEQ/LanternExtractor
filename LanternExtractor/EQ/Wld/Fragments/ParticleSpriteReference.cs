using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public class ParticleSpriteReference : WldFragment
    {
        private ParticleSprite _reference;
        public override void Initialize(int index, FragmentType id, int size, byte[] data, List<WldFragment> fragments, Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            int fragmentRef = Reader.ReadInt32();
            int value_08 = Reader.ReadInt32(); // always 0
            _reference = fragments[fragmentRef - 1] as ParticleSprite;
        }
    }
}