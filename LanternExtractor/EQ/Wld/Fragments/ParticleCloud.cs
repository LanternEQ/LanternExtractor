using System;
using System.Collections.Generic;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// ParticleCloud (0x34)
    /// Internal name: _PCD
    /// Defines a particle system. Can be referenced from a skeleton bone.
    /// </summary>
    public class ParticleCloud : WldFragment
    {
        public ParticleMovement ParticleMovement { get; private set; }
        public int Flags { get; private set; }
        public int SimultaneousParticles { get; private set; }
        public float SpawnRadius { get; private set; }
        public float SpawnAngle { get; private set; }
        public int SpawnLifespan { get; private set; }
        public float SpawnVelocity { get; private set; }
        public vec3 SpawnNormal { get; private set; }
        public int SpawnRate { get; private set; }
        public float SpawnScale { get; private set; }
        public Color Color { get; private set; }
        public ParticleSprite ParticleSprite;

        public override void Initialize(int index, int size, byte[] data, List<WldFragment> fragments,
            Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];

            int value_04 = Reader.ReadInt32(); // always 4
            int value_08 = Reader.ReadInt32(); // always 3
            ParticleMovement = (ParticleMovement)Reader.ReadInt32(); // Values are 1, 3, or 4
            Flags = Reader.ReadInt32();
            SimultaneousParticles = Reader.ReadInt32(); // 200, 30, particle count?
            int value_24 = Reader.ReadInt32(); // always 0
            int value_28 = Reader.ReadInt32(); // always 0
            int value_32 = Reader.ReadInt32(); // always 0
            int value_36 = Reader.ReadInt32(); // always 0
            int value_40 = Reader.ReadInt32(); // always 0
            SpawnRadius = Reader.ReadSingle(); // confirmed float
            SpawnAngle = Reader.ReadSingle(); // looks like a float
            SpawnLifespan = Reader.ReadInt32(); // looks like an int. numbers like 1000, 100, 750, 500, 1600, 2500.
            SpawnVelocity = Reader.ReadSingle(); // looks like a float. low numbers. 4, 5, 8, 10, 0
            SpawnNormal = new vec3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle());
            SpawnRate = Reader.ReadInt32(); // probably int 13, 15, 20, 600, 83? or bytes
            SpawnScale = Reader.ReadSingle(); // confirmed float 0.4, 0.5, 1.5, 0.1
            var colorBytes = BitConverter.GetBytes(Reader.ReadInt32());
            Color = new Color
            (
                colorBytes[2],
                colorBytes[1],
                colorBytes[0],
                colorBytes[3]
            );
            ParticleSprite = fragments[Reader.ReadInt32() - 1] as ParticleSprite;
        }
    }
}
