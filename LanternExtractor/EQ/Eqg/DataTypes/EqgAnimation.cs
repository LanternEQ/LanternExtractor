using System.Collections.Generic;
using System.Numerics;

namespace LanternExtractor.EQ.Eqg.DataTypes
{
    public struct EqgKeyframe
    {
        public int TimeMs;
        public Vector3 Translation;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public class EqgAnimationTrack
    {
        public string BoneName { get; set; }
        public List<EqgKeyframe> Frames { get; } = new List<EqgKeyframe>();
    }

    /// <summary>
    /// A parsed ANI file. Each track holds local bone transforms keyed in milliseconds.
    /// </summary>
    public class EqgAnimation
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public List<EqgAnimationTrack> Tracks { get; } = new List<EqgAnimationTrack>();
    }
}
