using System;
using System.Collections.Generic;
using LanternExtractor.EQ.Wld.DataTypes;
using Serilog;

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

        public override void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat);

            // Color is in BGRA format. A is always 255.
            var colorBytes = BitConverter.GetBytes(Reader.ReadInt32());
            Color = new Color
            (
                colorBytes[2],
                colorBytes[1],
                colorBytes[0],
                colorBytes[3]
            );
        }

        public override void OutputInfo()
        {
            base.OutputInfo();
            Log.Information("-----");
            Log.Information("GlobalAmbientLight: Color: " + Color);
        }
    }
}
