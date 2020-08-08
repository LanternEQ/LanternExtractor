using System.Collections.Generic;
using GlmSharp;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.DataTypes
{
    /// <summary>
    /// A node in the skeleton tree
    /// </summary>
    public class SkeletonNode
    {
        public int Index;
        public string Name;

        public string FullPath;

        // Track fragment (TrackFragment) 0x13
        public TrackFragment Track;
        
        // MeshFragment (MeshFragment) 0x2D
        public MeshReference MeshReference;

        // The children indices in the tree
        public List<int> Children;
    }

    public class BoneTransform
    {
        // translation
        public vec3 Translation;

        // rotation
        public quat Rotation;
        public vec4 Rotation2;
        public vec3 Rotation3;

        public float Scale;

        public float padding;
    }
}
