using System.Collections.Generic;
using System.Numerics;

namespace LanternExtractor.EQ.Eqg.DataTypes
{
    public struct EqgVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
        public Vector4 Color;
    }

    public struct EqgTriangle
    {
        public int V0;
        public int V1;
        public int V2;
        public int MaterialIndex;
        public uint Flags;
    }

    public class EqgBone
    {
        public string Name { get; set; }
        public int NextSibling { get; set; }
        public int ChildCount { get; set; }
        public int FirstChild { get; set; }
        public int Parent { get; set; } = -1;
        public Vector3 Translation { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
    }

    /// <summary>
    /// Up to four bone influences for a single vertex
    /// </summary>
    public struct EqgVertexWeights
    {
        public int Count;
        public int Bone0, Bone1, Bone2, Bone3;
        public float Weight0, Weight1, Weight2, Weight3;
    }

    /// <summary>
    /// A single mesh within a model. MOD and TER files contain one, MDS files can contain several
    /// (e.g. body and head pieces) that all share the same skeleton.
    /// </summary>
    public class EqgMeshPiece
    {
        public string Name { get; set; }
        public List<EqgVertex> Vertices { get; } = new List<EqgVertex>();
        public List<EqgTriangle> Triangles { get; } = new List<EqgTriangle>();
        public EqgVertexWeights[] Weights { get; set; }
        public bool HasVertexColors { get; set; }
    }

    public enum EqgModelType
    {
        Mod,
        Mds,
        Ter
    }

    /// <summary>
    /// A parsed MOD, MDS or TER file
    /// </summary>
    public class EqgModel
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public EqgModelType Type { get; set; }
        public int Version { get; set; }
        public List<EqgMaterial> Materials { get; } = new List<EqgMaterial>();
        public List<EqgBone> Bones { get; } = new List<EqgBone>();
        public List<EqgMeshPiece> Pieces { get; } = new List<EqgMeshPiece>();

        /// <summary>
        /// Many static MOD files (e.g. items) contain only a root bone and prop attachment points,
        /// with every vertex weighted to the root. A model is only treated as skinned if it is an MDS
        /// or a vertex is influenced by a bone that has a non-identity bind pose.
        /// </summary>
        public bool IsSkinned()
        {
            if (Bones.Count == 0)
            {
                return false;
            }

            if (Type == EqgModelType.Mds)
            {
                return true;
            }

            var worldMatrices = GetBoneWorldMatrices();
            foreach (var piece in Pieces)
            {
                if (piece.Weights == null)
                {
                    continue;
                }

                foreach (var weights in piece.Weights)
                {
                    if (IsDeformingBone(worldMatrices, weights.Bone0, weights.Weight0) ||
                        IsDeformingBone(worldMatrices, weights.Bone1, weights.Weight1) ||
                        IsDeformingBone(worldMatrices, weights.Bone2, weights.Weight2) ||
                        IsDeformingBone(worldMatrices, weights.Bone3, weights.Weight3))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates the model space matrix of every bone in the bind pose
        /// </summary>
        public Matrix4x4[] GetBoneWorldMatrices()
        {
            var locals = new Matrix4x4[Bones.Count];
            for (var i = 0; i < Bones.Count; i++)
            {
                locals[i] = CreateBoneMatrix(Bones[i].Translation, Bones[i].Rotation, Bones[i].Scale);
            }

            return GetBoneWorldMatrices(locals);
        }

        /// <summary>
        /// Combines local bone matrices (in bone order) into model space matrices using the bone hierarchy
        /// </summary>
        public Matrix4x4[] GetBoneWorldMatrices(Matrix4x4[] localMatrices)
        {
            var matrices = new Matrix4x4[Bones.Count];
            var resolved = new bool[Bones.Count];

            for (var i = 0; i < Bones.Count; i++)
            {
                Resolve(i, 0);
            }

            return matrices;

            Matrix4x4 Resolve(int index, int depth)
            {
                if (resolved[index])
                {
                    return matrices[index];
                }

                var parent = Bones[index].Parent;
                var hasParent = parent >= 0 && depth < Bones.Count;
                matrices[index] = hasParent ? localMatrices[index] * Resolve(parent, depth + 1) : localMatrices[index];
                resolved[index] = true;
                return matrices[index];
            }
        }

        public static Matrix4x4 CreateBoneMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            return Matrix4x4.CreateScale(scale)
                   * Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(rotation))
                   * Matrix4x4.CreateTranslation(translation);
        }

        private static bool IsDeformingBone(Matrix4x4[] worldMatrices, int bone, float weight)
        {
            return weight > 0f && bone >= 0 && bone < worldMatrices.Length && !worldMatrices[bone].IsIdentity;
        }

        public void ResolveBoneParents()
        {
            for (var i = 0; i < Bones.Count; i++)
            {
                var child = Bones[i].FirstChild;
                var visited = 0;
                while (child >= 0 && child < Bones.Count && visited < Bones[i].ChildCount)
                {
                    if (Bones[child].Parent != -1)
                    {
                        break;
                    }

                    Bones[child].Parent = i;
                    child = Bones[child].NextSibling;
                    visited++;
                }
            }
        }
    }
}
