using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x28 - Light Info
    /// Defines a position and radius of a light instance 
    /// </summary>
    class LightInfo : WldFragment
    {
        /// <summary>
        /// The light reference (0x1C) this fragment refers to
        /// </summary>
        public LightSourceReference LightReference { get; private set; }

        /// <summary>
        /// The position of this light
        /// </summary>
        public vec3 Position { get; private set; }

        /// <summary>
        /// The radius of the light
        /// </summary>
        public float Radius { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            LightReference = fragments[reference - 1] as LightSourceReference;

            int flags = reader.ReadInt32();

            Position = new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            Radius = reader.ReadSingle();
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x28: Reference: " + (LightReference.Index + 1));
            logger.LogInfo("0x28: Position: " + Position);
            logger.LogInfo("0x28: Radius: " + Radius);
        }
    }
}