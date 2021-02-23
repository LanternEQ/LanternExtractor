using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class AnimationWriter : TextAssetWriter
    {
        private string _targetAnimation;
        private bool _isCharacterAnimation;
        public AnimationWriter(bool isCharacterAnimation)
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Animation");
            _isCharacterAnimation = isCharacterAnimation;
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

            if (!skeleton.Animations.ContainsKey(_targetAnimation))
            {
                return;
            }

            Animation anim = skeleton.Animations[_targetAnimation];

            if (skeleton.Name.ToLower().Contains("it153") && _targetAnimation == "c05")
            {
                
            }
            
            _export.AppendLine("# Animation: " + _targetAnimation);
            _export.AppendLine("framecount," + anim.FrameCount);
            _export.AppendLine("totalTimeMs," + anim.AnimationTimeMs);
            
            
            if(skeleton.Name.Contains("IT4"))
            {}
            

            for (int i = 0; i < skeleton.Tree.Count; ++i)
            {
                string boneName = _isCharacterAnimation ? skeleton.BoneMappingClean[i].ToLower() : skeleton.BoneMapping[i].ToLower();
                
                if (boneName == "")
                {
                    continue;
                }

                string fullPath = _isCharacterAnimation ? skeleton.Tree[i].FullPath : skeleton.Tree[i].CleanedFullPath;

                if (!anim.Tracks.ContainsKey(boneName))
                {
                    if (_isCharacterAnimation)
                    {
                        if (!skeleton.Animations["pos"].TracksCleaned.ContainsKey(boneName))
                        {
                            return;
                        }
                    
                        var bt = skeleton.Animations["pos"]?.TracksCleaned[boneName]?.TrackDefFragment.Frames2[0];

                        if (bt == null)
                        {
                            return;
                        }
                        CreateTrackString(fullPath, 0, bt, anim.AnimationTimeMs);
                        continue;
                    }
                    else
                    {
                        if (!skeleton.Animations["pos"].Tracks.ContainsKey(boneName))
                        {
                            return;
                        }
                    
                        var bt = skeleton.Animations["pos"]?.Tracks[boneName]?.TrackDefFragment.Frames2[0];

                        if (bt == null)
                        {
                            return;
                        }
                        CreateTrackString(fullPath, 0, bt, anim.AnimationTimeMs);
                        continue;
                    }
                }
                
                for (int j = 0; j < anim.FrameCount; ++j)
                {

                    if (j >= anim.Tracks[boneName].TrackDefFragment.Frames2.Count)
                    {
                        break;
                    }

                    BoneTransform boneTransform = anim.Tracks[boneName].TrackDefFragment.Frames2[j];

                    int delay = 0;
                    
                    if (_isCharacterAnimation)
                    {
                        delay = anim.AnimationTimeMs / anim.FrameCount;
                    }
                    else
                    {
                        delay = skeleton.Tree[i].Track.FrameMs;
                    }

                    CreateTrackString(fullPath, j, boneTransform, delay);
                }
            }
        }
        
        private void CreateTrackString(string fullPath, int frame, BoneTransform boneTransform, int delay)
        {
            _export.Append(fullPath);
            _export.Append(",");

            _export.Append(frame);
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
            
            _export.Append(delay.ToString());

            _export.AppendLine();
        }
    }
}