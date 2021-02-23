using System.Collections.Generic;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.DataTypes
{
    public class Animation
    {
        public string AnimModelBase;
        public Dictionary<string, TrackFragment> Tracks;
        public Dictionary<string, TrackFragment> TracksCleaned;
        public int FrameCount;
        public int AnimationTimeMs { get; set; }

        public Animation()
        {
            Tracks = new Dictionary<string, TrackFragment>();
            TracksCleaned = new Dictionary<string, TrackFragment>();
        }

        public static string CleanBoneName(string name, string modelBase)
        {
            name = name.ToLower();
            name = name.Replace("_dag", "");
            name = name.Replace(modelBase, string.Empty);
            name += name.Length == 0 ? "root" : string.Empty;
            return name;
        }
        
        public void AddTrack(TrackFragment track, string pieceName, string cleanName)
        {
            // Prevent overwriting tracks
            // Drachnid edge case
            if (Tracks.ContainsKey(pieceName))
            {
                return;
            }
            
            Tracks[pieceName.ToLower()] = track;
            TracksCleaned[cleanName.ToLower()] = track;

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