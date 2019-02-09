using GlmSharp;

namespace LanternExtractor.EQ.Wld.DataTypes
{
    public class BonePosition
    {
        public vec3 Translation { get; set; }
        public quat Rotation { get; set; }
    }
}