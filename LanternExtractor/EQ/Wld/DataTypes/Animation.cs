using System.Collections.Generic;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.DataTypes
{
    public class Animation
    {
        public string AnimModelBase;
        public Dictionary<string, TrackFragment> Tracks;
        public int FrameCount;
        public int AnimationTimeMs { get; set; }

        public Animation()
        {
            Tracks = new Dictionary<string, TrackFragment>();
        }

        public void AddTrack(TrackFragment track, string pieceName)
        {
            string trackName = track.Name;

            Tracks[pieceName] = track;

            if (string.IsNullOrEmpty(AnimModelBase) &&
                !string.IsNullOrEmpty(track.ModelName))
            {
                AnimModelBase = track.ModelName;
            }
             
            if (track.TrackDefFragment.Frames2.Count > FrameCount)
            {
                FrameCount = track.TrackDefFragment.Frames2.Count;
            }

            int totalTime = track.TrackDefFragment.Frames2.Count * track.FrameMs;

            if (totalTime > AnimationTimeMs)
            {
                AnimationTimeMs = totalTime;
            }
        }
    }
}