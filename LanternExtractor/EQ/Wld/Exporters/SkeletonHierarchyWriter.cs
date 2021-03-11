using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class SkeletonHierarchyWriter : TextAssetWriter
    {
        public override void AddFragmentData(WldFragment data)
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Skeleton Hierarchy");
            _export.AppendLine(LanternStrings.ExportHeaderFormat + "BoneName, Children, Mesh, ParticleCloud");
            
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

                _export.Append(node.CleanedName);
                _export.Append(",");
                _export.Append(childrenList);

                _export.Append(",");

                if (node.MeshReference?.Mesh != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(node.MeshReference.Mesh));
                }
                
                if (node.MeshReference?.LegacyMesh != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(node.MeshReference.LegacyMesh));
                }
                
                _export.Append(",");

                if (node.ParticleCloud != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(node.ParticleCloud));
                }
                
                _export.AppendLine();
            }
        }
    }
}