using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class SkeletonHierarchyNewWriter : TextAssetWriter
    {
        private bool _stripModelBase;
        
        public SkeletonHierarchyNewWriter(bool stripModelBase)
        {
            _stripModelBase = stripModelBase;
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Skeleton Hierarchy");
            _export.AppendLine(LanternStrings.ExportHeaderFormat + "BoneName, Children, Mesh, AlternateMesh, ParticleCloud");
            
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

                var boneName = CleanSkeletonNodeName(node.Name);

                if (_stripModelBase)
                {
                    boneName = StripModelBase(boneName, skeleton.ModelBase);
                }
                
                _export.Append(CleanSkeletonNodeName(boneName));
                _export.Append(",");
                _export.Append(childrenList);

                _export.Append(",");

                if (node.MeshReference?.Mesh != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(node.MeshReference.Mesh));
                }
                
                _export.Append(",");

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
        
        private string CleanSkeletonNodeName(string name)
        {
            return name.Replace("_DAG", "").ToLower();
        }

        private string StripModelBase(string boneName, string modelBase)
        {
            if (boneName.StartsWith(modelBase))
            {
                boneName = boneName.Substring(modelBase.Length);
            }

            if (string.IsNullOrEmpty(boneName))
            {
                boneName = "root";
            }

            return boneName;
        }
    }
}