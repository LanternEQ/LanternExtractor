using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class SkeletonHierarchyExporter : TextAssetExporter
    {
        public SkeletonHierarchyExporter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Skeleton Hierarchy");
            _export.AppendLine(LanternStrings.ExportHeaderFormat + "BoneName, Children, Mesh");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            SkeletonHierarchy skeleton = data as SkeletonHierarchy;

            if (skeleton == null)
            {
                return;
            }
            
            foreach (var node in skeleton.Tree)
            {
                string childrenList = string.Empty;
                
                foreach (var children in node.Children)
                {
                    childrenList += children;

                    if (children != node.Children.Last())
                    {
                        childrenList += ";";
                    }
                }

                _export.Append(node.Name.Replace("_DAG", ""));
                _export.Append(",");
                _export.Append(childrenList);

                if (node.MeshReference?.Mesh != null)
                {
                    _export.Append(",");
                    _export.Append(FragmentNameCleaner.CleanName(node.MeshReference.Mesh));
                }
                
                _export.AppendLine();
            }
        }
    }
}