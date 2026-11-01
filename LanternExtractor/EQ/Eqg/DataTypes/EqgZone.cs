using System.Collections.Generic;
using System.Numerics;

namespace LanternExtractor.EQ.Eqg.DataTypes
{
    /// <summary>
    /// A model instance placed in a zone. Rotation is in radians.
    /// </summary>
    public class EqgPlacement
    {
        public string ModelFileName { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public float Scale { get; set; }

        /// <summary>
        /// Transforms a model from its Z up model space into the Y up zone space
        /// </summary>
        public Matrix4x4 GetTransform()
        {
            return Matrix4x4.CreateScale(Scale == 0f ? 1f : Scale)
                   * Matrix4x4.CreateRotationX(Rotation.X)
                   * Matrix4x4.CreateRotationY(Rotation.Y)
                   * Matrix4x4.CreateRotationZ(Rotation.Z)
                   * Matrix4x4.CreateTranslation(Position);
        }
    }

    public class EqgLight
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Color { get; set; }
        public float Radius { get; set; }
    }

    /// <summary>
    /// A parsed binary ZON file (EQGZ)
    /// </summary>
    public class EqgZone
    {
        public string Name { get; set; }
        public List<string> ModelFileNames { get; } = new List<string>();
        public List<EqgPlacement> Placements { get; } = new List<EqgPlacement>();
        public List<EqgLight> Lights { get; } = new List<EqgLight>();
    }
}
