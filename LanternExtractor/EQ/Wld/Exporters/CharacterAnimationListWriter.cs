using System.Text;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class CharacterAnimationListWriter : TextAssetWriter
    {
        public CharacterAnimationListWriter()
        {
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
                _export.AppendLine(filename);
            }
        }
    }
}