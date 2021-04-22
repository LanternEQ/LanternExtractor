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
        public string CleanedName;
        public string CleanedFullPath;
        public List<int> Children;
        public TrackFragment Track;
        public mat4 PoseMatrix;
        public MeshReference MeshReference;
        public ParticleCloud ParticleCloud { get; set; }
        public Dictionary<string, TrackFragment> AnimationTracks { get; set; }
    }
}
