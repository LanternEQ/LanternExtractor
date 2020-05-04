using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class AnimatedMeshListExporter : TextAssetExporter
    {
        public AnimatedMeshListExporter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Animated Mesh List");
            _export.AppendLine("# Total animated meshes: ");        
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            if (!(data is SkeletonHierarchy))
            {
                return;
            }

            _export.AppendLine(FragmentNameCleaner.CleanName(data));
        }
    }
}