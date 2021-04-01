using System.Collections.Generic;
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

        // Track fragment (TrackFragment) 0x13
        public TrackFragment Track;
        
        // MeshFragment (MeshFragment) 0x2D        Residential
        public MeshReference MeshReference;

        // The children indices in the tree
        public List<int> Children;
        public ParticleCloud ParticleCloud { get; set; }
        
        public List<int> ConnectedPieces { get; set; }
        public Dictionary<string, TrackFragment> AnimationTracks { get; set; }
    }
}
