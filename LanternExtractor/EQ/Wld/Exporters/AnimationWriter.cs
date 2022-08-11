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
            if (!(data is SkeletonHierarchy skeleton))
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

            _export.AppendLine("# Animation: " + _targetAnimation);
            _export.AppendLine("framecount," + anim.FrameCount);
            _export.AppendLine("totalTimeMs," + anim.AnimationTimeMs);

            for (int i = 0; i < skeleton.Skeleton.Count; ++i)
            {
                string boneName = Animation.CleanBoneAndStripBase(skeleton.BoneMapping[i], skeleton.ModelBase);
                string fullPath = skeleton.Skeleton[i].CleanedFullPath;

                var trackArray = anim.TracksCleanedStripped;
                var poseArray = skeleton.Animations["pos"].TracksCleanedStripped;

                if (!trackArray.ContainsKey(boneName))
                {
                    if (poseArray == null || !poseArray.ContainsKey(boneName))
                    {
                        return;
                    }

                    var bt = poseArray[boneName].TrackDefFragment.Frames[0];

                    if (bt == null)
                    {
                        return;
                    }

                    CreateTrackString(fullPath, 0, bt, anim.AnimationTimeMs);
                }
                else
                {
                    for (int j = 0; j < anim.FrameCount; ++j)
                    {
                        if (j >= trackArray[boneName].TrackDefFragment.Frames.Count)
                        {
                            break;
                        }

                        BoneTransform boneTransform = trackArray[boneName].TrackDefFragment.Frames[j];
                        int delay = _isCharacterAnimation
                            ? anim.AnimationTimeMs / anim.FrameCount
                            : skeleton.Skeleton[i].Track.FrameMs;
                        CreateTrackString(fullPath, j, boneTransform, delay);
                    }
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


        private string StripModelBase(string boneName, string modelBase)
        {
            if (boneName.StartsWith(modelBase + "/"))
            {
                boneName = boneName.Substring(modelBase.Length);
                boneName = boneName.Insert(0, "root");
            }


            return boneName;
        }
    }
}
