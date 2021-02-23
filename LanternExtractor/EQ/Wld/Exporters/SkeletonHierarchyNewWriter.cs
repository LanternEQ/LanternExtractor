using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class SkeletonHierarchyNewWriter : TextAssetWriter
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
            
            if (skeleton.Meshes != null && skeleton.Meshes.Count != 0)
            {
                _export.Append("meshes,");
                foreach (var mesh in skeleton.Meshes)
                {
                    _export.Append(FragmentNameCleaner.CleanName(mesh));
                }
                
                _export.AppendLine();
            }
            
            if (skeleton.AlternateMeshes != null && skeleton.AlternateMeshes.Count != 0)
            {
                _export.Append("meshes,");
                foreach (var mesh in skeleton.AlternateMeshes)
                {
                    _export.Append(FragmentNameCleaner.CleanName(mesh));
                }
                
                _export.AppendLine();
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

                _export.Append(node.Name.Replace("_DAG", "").ToLower());
                _export.Append(",");
                _export.Append(childrenList);

                _export.Append(",");

                if (node.MeshReference?.Mesh != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(node.MeshReference.Mesh));
                }
                
                if (node.MeshReference?.AlternateMesh != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(node.MeshReference.AlternateMesh));
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