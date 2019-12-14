using System.Collections.Generic;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.DataTypes
{
    public class SkeletonPieceData
    {
        public string Name { get; set; }
        public List<int> ConnectedPieces { get; set; }
        public Dictionary<string, TrackFragment> AnimationTracks { get; set; }
    }
}