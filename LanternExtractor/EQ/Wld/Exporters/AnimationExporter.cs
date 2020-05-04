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
            ;
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

            for (int i = 0; i < skeleton.Tree.Count; ++i)
            {
                for (int j = 0; j < frameCount; ++j)
                {
                    _export.Append(skeleton.Tree[i].FullPath);
                    _export.Append(",");

                    _export.Append(j);
                    _export.Append(",");

                    int frame = j;

                    if (skeleton.Tree[i].Track.TrackDefFragment.Frames2.Count == 1)
                    {
                        frame = 0;
                    }

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.x);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.z);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.y);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.x);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.z);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.y);
                    _export.Append(",");

                    _export.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.w);
                    _export.AppendLine();
                }
            }
        }
    }
}