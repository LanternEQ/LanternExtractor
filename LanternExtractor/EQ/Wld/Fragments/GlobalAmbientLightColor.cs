using System;
using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Global Ambient Light (0x35)
    /// Contains the color value which is added to boost the darkness in some zone.
    /// This fragment contains no name reference and is only found in the zone WLD (e.g. akanon.wld)
    /// </summary>
    public class GlobalAmbientLightColor : WldFragment
    {
        public Color Color { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            // Color is in BGRA format. A is always 255.
            int colorValue = reader.ReadInt32();
            byte[] colorBytes = BitConverter.GetBytes(colorValue);

            Color = new Color {R = colorBytes[2], G = colorBytes[1], B = colorBytes[0], A = colorBytes[3]};
        }
    }
}