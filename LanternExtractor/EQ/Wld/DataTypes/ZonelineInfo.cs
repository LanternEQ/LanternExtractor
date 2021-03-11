using GlmSharp;

namespace LanternExtractor.EQ.Wld.DataTypes
{
    public class ZonelineInfo
    {
        public ZonelineType Type;
        public int Index;
        public vec3 Position;
        public int Heading;
        public int ZoneIndex { get; set; }
    }
}