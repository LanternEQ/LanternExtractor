using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class GlobalAnimationListWriter : TextAssetWriter
    {
        private List<string> animations = new List<string>();
        
        public GlobalAnimationListWriter()
        {
            if (!File.Exists("all/character_animations.txt"))
            {
                return;
            }
            
            var text = File.ReadAllLines("all/character_animations.txt");

            foreach (var line in text)
            {
                animations.Add(line);
            }
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            SkeletonHierarchy skeleton = data as SkeletonHierarchy;

            if (skeleton == null)
            {
                return;
            }

            foreach (var animation in skeleton.Animations)
            {
                var modelBase = string.IsNullOrEmpty(animation.Value.AnimModelBase)
                    ? skeleton.ModelBase
                    : animation.Value.AnimModelBase;
                string filename = modelBase + "_" + animation.Key;

                if (animations.Contains(filename))
                {
                    return;
                }
                
                animations.Add(filename);
            }
        }

        public override void WriteAssetToFile(string fileName)
        {
            animations.Sort();
            
            foreach (var animation in animations)
            {
                _export.AppendLine(animation);
            }
            
            base.WriteAssetToFile(fileName);
        }
    }
}