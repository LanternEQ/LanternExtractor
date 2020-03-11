using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x1B - Light Source
    /// Defines color information about a light
    /// </summary>
    class LightSource : WldFragment
    {
        /// <summary>
        /// Is a placed light source if used in the light.wld and is not if used in the main zone file
        /// </summary>
        public bool IsPlacedLightSource { get; private set; }

        /// <summary>
        /// Is this a colored light? If so, the fragment size is larger
        /// </summary>
        public bool IsColoredLight { get; private set; }

        /// <summary>
        /// The color of the light - if it is colored
        /// </summary>
        public vec4 Color { get; private set; }

        /// <summary>
        /// The attenuation (?) - guess from Windcatcher. Not sure what it is.
        /// </summary>
        public int Attenuation { get; private set; }
        public float SomeValue { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            var bitAnalyzer = new BitAnalyzer(flags);

            if (bitAnalyzer.IsBitSet(1))
            {
                IsPlacedLightSource = true;
            }

            if (bitAnalyzer.IsBitSet(4))
            {
                IsColoredLight = true;
            }

            if (!IsPlacedLightSource)
            {
                if (!IsColoredLight)
                {
                    int something1 = reader.ReadInt32();
                    SomeValue = reader.ReadSingle();

                    // Always 1 and 1.0f
                    if (something1 != 1 || SomeValue < 1.0f)
                    {
                        
                    }
                    
                    return;
                }
                else
                {
                    Attenuation = reader.ReadInt32();

                    float alpha = reader.ReadSingle();
                    float red = reader.ReadSingle();
                    float green = reader.ReadSingle();
                    float blue = reader.ReadSingle();
                    Color = new vec4(red, green, blue, alpha);

                    if (Attenuation != 1)
                    {
                        
                    }
                }

                return;
            }

            if (!IsColoredLight)
            {
                int something1 = reader.ReadInt32();
                float something2 = reader.ReadSingle();

                //float something3 = reader.ReadSingle();
                //float something4 = reader.ReadSingle();
                //float something5 = reader.ReadSingle();
                return;
            }
            

            // Not sure yet what the purpose of this fragment is in the main zone file
            // For now, return
            if (!IsPlacedLightSource && Name == "DEFAULT_LIGHTDEF")
            {
                int unknown = reader.ReadInt32();

                float unknown6 = reader.ReadSingle();

                return;
            }

            int unknown1 = reader.ReadInt32();

            if (!IsColoredLight)
            {
                int unknown = reader.ReadInt32();
                Color = new vec4(1.0f);
                
                int unknown2 = reader.ReadInt32();
                int unknown3 = reader.ReadInt32();

            }
            else
            {
                Attenuation = reader.ReadInt32();

                float alpha = reader.ReadSingle();
                float red = reader.ReadSingle();
                float green = reader.ReadSingle();
                float blue = reader.ReadSingle();

                Color = new vec4(red, green, blue, alpha);
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x1B: Is a placed light: " + IsPlacedLightSource);
            logger.LogInfo("0x1B: Is a colored light: " + IsColoredLight);

            if (IsColoredLight)
            {
                logger.LogInfo("0x1B: Color: " + Color);
                logger.LogInfo("0x1B: Attenuation (?): " + Attenuation);
            }
        }
    }
}