using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class CharacterAnimationGlobalListWriter : TextAssetWriter
    {
        private List<string> _animations = new List<string>();
        
        public CharacterAnimationGlobalListWriter()
        {
            if (!File.Exists("all/character_animations.txt"))
            {
                return;
            }
            
            var text = File.ReadAllLines("all/character_animations.txt");

            foreach (var line in text)
            {
                _animations.Add(line);
            }
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            SkeletonHierarchy skeleton = data as SkeletonHierarchy;

            if (skeleton == null)
            {
                return;
            }

            if (skeleton.Name.StartsWith("OGM") || skeleton.Name.StartsWith("TRM"))
            {
                
            }

            foreach (var animation in skeleton.Animations)
            {
                var modelBase = string.IsNullOrEmpty(animation.Value.AnimModelBase)
                    ? skeleton.ModelBase
                    : animation.Value.AnimModelBase;
                string filename = modelBase + "_" + animation.Key;

                if (_animations.Contains(filename))
                {
                    continue;
                }
                
                _animations.Add(filename);
            }
        }

        public override void WriteAssetToFile(string fileName)
        {
            _animations.Sort();
            
            foreach (var animation in _animations)
            {
                _export.AppendLine(animation);
            }
            
            base.WriteAssetToFile(fileName);
        }
    }
}