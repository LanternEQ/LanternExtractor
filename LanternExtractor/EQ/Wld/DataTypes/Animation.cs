using System.Collections.Generic;
using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.DataTypes
{
    public class Animation
    {
        public string AnimModelBase;
        public Dictionary<string, TrackFragment> Tracks;
        public Dictionary<string, TrackFragment> TracksCleaned;
        public Dictionary<string, TrackFragment> TracksCleanedStripped;
        public int FrameCount;
        public int AnimationTimeMs { get; set; }

        public Animation()
        {
            Tracks = new Dictionary<string, TrackFragment>();
            TracksCleaned = new Dictionary<string, TrackFragment>();
            TracksCleanedStripped = new Dictionary<string, TrackFragment>();
        }
        
        public static string CleanBoneName(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
            {
                return boneName;
            }
            
            var cleanedName = boneName.Replace("_DAG", string.Empty).ToLower();
            return cleanedName.Length == 0 ? "root" : cleanedName;
        }
        
        public static string CleanBoneAndStripBase(string boneName, string modelBase)
        {
            var cleanedName = boneName.Replace("_DAG", string.Empty).ToLower();

            if (cleanedName.StartsWith(modelBase))
            {
                cleanedName = cleanedName.Substring(modelBase.Length);
            }
            
            return cleanedName.Length == 0 ? "root" : cleanedName;
        }

        public static string CleanBoneName(string name, string modelBase)
        {
            name = name.ToLower();
            name = name.Replace("_dag", "");
            name = name.Replace(modelBase, string.Empty);
            name += name.Length == 0 ? "root" : string.Empty;
            return name;
        }
        
        public void AddTrack(TrackFragment track, string pieceName, string cleanedName, string cleanStrippedName)
        {
            // Prevent overwriting tracks
            // Drachnid edge case
            if (Tracks.ContainsKey(pieceName))
            {
                return;
            }
            
            Tracks[pieceName] = track;
            TracksCleaned[cleanedName] = track;
            TracksCleanedStripped[cleanStrippedName] = track;

            if (string.IsNullOrEmpty(AnimModelBase) &&
                !string.IsNullOrEmpty(track.ModelName))
            {
                AnimModelBase = track.ModelName;
            }
             
            if (track.TrackDefFragment.Frames.Count > FrameCount)
            {
                FrameCount = track.TrackDefFragment.Frames.Count;
            }

            int totalTime = track.TrackDefFragment.Frames.Count * track.FrameMs;

            if (totalTime > AnimationTimeMs)
            {
                AnimationTimeMs = totalTime;
            }
        }
    }
}