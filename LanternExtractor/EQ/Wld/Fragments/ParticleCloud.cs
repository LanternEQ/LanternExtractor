using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// ParticleCloud (0x34)
    /// Internal name: None
    /// Defines a particle system. Can be referenced from a skeleton bone.
    /// </summary>
    public class ParticleCloud : WldFragment
    {
        private ParticleSprite _particleSprite;

        public override void Initialize(int index, FragmentType id, int size, byte[] data, List<WldFragment> fragments,
            Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            
            //File.WriteAllBytes("ParticleClouds/" + Name, data);

            int flags = Reader.ReadInt32(); // always 4
            int value_08 = Reader.ReadInt32(); // always 3
            int value_12 = Reader.ReadInt32(); // Values are 1, 3, or 4
            byte value_16 = Reader.ReadByte();
            byte value_17 = Reader.ReadByte();
            byte value_18 = Reader.ReadByte();
            byte value_19 = Reader.ReadByte();
            int value_20 = Reader.ReadInt32(); // 200, 30, particle count? 
            int value_24 = Reader.ReadInt32(); // always 0
            int value_28 = Reader.ReadInt32(); // always 0
            int value_32 = Reader.ReadInt32(); // always 0
            int value_36 = Reader.ReadInt32(); // always 0
            int value_40 = Reader.ReadInt32(); // always 0
            float value_44 = Reader.ReadSingle(); // confirmed float
            float value_48 = Reader.ReadSingle(); // looks like a float
            int value_52 = Reader.ReadInt32(); // looks like an int. numbers like 1000, 100, 750, 500, 1600, 2500.
            float value_56 = Reader.ReadSingle(); // looks like a float. low numbers. 4, 5, 8, 10, 0
            float value_60 = Reader.ReadSingle(); // float 0 or 1
            float value_64 = Reader.ReadSingle(); // float 0 or -1
            float value_68 = Reader.ReadSingle(); // float 0 or -1
            int value_72 = Reader.ReadInt32(); // probably int 13, 15, 20, 600, 83? or bytes
            float value_76 = Reader.ReadSingle(); // confirmed float 0.4, 0.5, 1.5, 0.1
            float value_80 = Reader.ReadSingle(); // float 0.4, 1.9
            _particleSprite = fragments[Reader.ReadInt32() - 1] as ParticleSprite;
        }
    }
}