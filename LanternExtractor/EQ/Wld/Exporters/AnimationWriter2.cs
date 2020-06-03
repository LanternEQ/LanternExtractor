using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class AnimationWriter2 : TextAssetWriter
    {
        private string _targetAnimation;

        public AnimationWriter2()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Animation");
        }

        public void SetTargetAnimation(string animationName)
        {
            _targetAnimation = animationName;
        }

        public override void AddFragmentData(WldFragment data)
        {
            SkeletonHierarchy skeleton = data as SkeletonHierarchy;

            if (skeleton == null)
            {
                return;
            }

            if (_targetAnimation == string.Empty)
            {
                return;
            }

            if (!skeleton._animations.ContainsKey(_targetAnimation))
            {
                return;
            }

            Animation2 anim = skeleton._animations[_targetAnimation];
            
            _export.AppendLine("# Animation Test: " + _targetAnimation);
            _export.AppendLine("framecount," + anim.FrameCount);
            if (skeleton.AnimationDelayList.ContainsKey(_targetAnimation))
            {
                _export.AppendLine("time," + skeleton.AnimationDelayList[_targetAnimation]);
            }
            else
            {
                _export.AppendLine("time,0");
            }
            
            for (int i = 0; i < skeleton.Tree.Count; ++i)
            {
                string boneName = skeleton._boneNameMapping[i];

                if (boneName == "")
                {
                    continue;
                }
                
                if (!anim.Tracks.ContainsKey(boneName))
                {
                    continue;
                }
                
                for (int j = 0; j < anim.FrameCount; ++j)
                {

                    if (j >= anim.Tracks[boneName].TrackDefFragment.Frames2.Count)
                    {
                        break;
                    }

                    BoneTransform boneTransform = anim.Tracks[boneName].TrackDefFragment.Frames2[j];
                    
                    _export.Append(skeleton.Tree[i].FullPath);
                    _export.Append(",");

                    _export.Append(j);
                    _export.Append(",");

                    _export.Append(boneTransform.Translation.x);
                    _export.Append(",");

                    _export.Append(boneTransform.Translation.z);
                    _export.Append(",");

                    _export.Append(boneTransform.Translation.y);
                    _export.Append(",");

                    _export.Append(-boneTransform.Rotation.x);
                    _export.Append(",");

                    _export.Append(-boneTransform.Rotation.z);
                    _export.Append(",");

                    _export.Append(-boneTransform.Rotation.y);
                    _export.Append(",");

                    _export.Append(boneTransform.Rotation.w);
                    _export.Append(",");
                    
                    _export.Append(boneTransform.Scale);
                    _export.Append(",");
                    
                    _export.Append(skeleton.Tree[i].Track.FrameMs);
                    _export.AppendLine();
                }
            }
        }
    }
}