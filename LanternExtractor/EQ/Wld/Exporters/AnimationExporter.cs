using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class AnimationExporter : TextAssetExporter
    {
        private string _targetAnimation;

        public AnimationExporter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Animation Test");
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

            if (!skeleton.AnimationList.ContainsKey(_targetAnimation))
            {
                return;
            }

            int frameCount = skeleton.AnimationList[_targetAnimation];

            _export.AppendLine("# Animation Test: " + _targetAnimation);
            _export.AppendLine("framecount," + frameCount);
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
                for (int j = 0; j < frameCount; ++j)
                {
                    if (j >= skeleton.Tree[i].Track.TrackDefFragment.Frames2.Count)
                    {
                        break;
                    }
                    
                    _export.Append(skeleton.Tree[i].FullPath);
                    _export.Append(",");

                    _export.Append(j);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Translation.x);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Translation.z);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Translation.y);
                    _export.Append(",");

                    _export.Append(-skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Rotation.x);
                    _export.Append(",");

                    _export.Append(-skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Rotation.z);
                    _export.Append(",");

                    _export.Append(-skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Rotation.y);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Rotation.w);
                    _export.Append(",");
                    
                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[j].Scale);
                    _export.Append(",");
                    
                    _export.Append(skeleton.Tree[i].Track.FrameMs);
                    _export.AppendLine();
                }
            }
        }
    }
}