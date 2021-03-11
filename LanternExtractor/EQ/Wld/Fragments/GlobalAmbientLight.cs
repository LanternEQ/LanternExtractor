using System;
using System.Collections.Generic;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// GlobalAmbientLight (0x35)
    /// Internal name: None
    /// Contains the color value which is added to boost the darkness in some zone.
    /// This fragment contains no name reference and is only found in zone WLDs (e.g. akanon.wld).
    /// </summary>
    public class GlobalAmbientLight : WldFragment
    {
        public Color Color { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            
            // Color is in BGRA format. A is always 255.
            var colorBytes = BitConverter.GetBytes(Reader.ReadInt32());
            Color = new Color
            {
                R = colorBytes[2],
                G = colorBytes[1],
                B = colorBytes[0],
                A = colorBytes[3]
            };
        }
        
        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("GlobalAmbientLight: Color: " + Color);
        }
    }
}