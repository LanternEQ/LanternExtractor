using System.Text;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ActorWriter : TextAssetWriter
    {
        private ActorType _actorType;
        private int _actorCount;
        public ActorWriter(ActorType actorType)
        {
            _actorType = actorType;
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            Actor actor = data as Actor;

            if (actor == null)
            {
                return;
            }
            
            if (actor.ActorType != _actorType)
            {
                return;
            }

            if (_actorType == ActorType.Skeletal)
            {
                _export.Append(FragmentNameCleaner.CleanName(actor));
                _export.Append(",");
                _export.Append(FragmentNameCleaner.CleanName(actor.SkeletonReference.SkeletonHierarchy));
                _export.AppendLine();
            }
            else if (_actorType == ActorType.Static)
            {
                _export.Append(FragmentNameCleaner.CleanName(actor));
                _export.Append(",");

                if (actor.MeshReference.Mesh != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(actor.MeshReference.Mesh));
                }
                else if (actor.MeshReference.LegacyMesh != null)
                {
                    _export.Append(FragmentNameCleaner.CleanName(actor.MeshReference.LegacyMesh));
                }
                
                _export.AppendLine();
            }
            else
            {
                _export.AppendLine(FragmentNameCleaner.CleanName(actor));
            }
            
            _actorCount++;
        }

        public override void WriteAssetToFile(string fileName)
        {
            if (_export.Length == 0)
            {
                return;
            }
            
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.AppendLine(LanternStrings.ExportHeaderTitle +
                                     "Actor List");
            headerBuilder.AppendLine("# Total models: " + _actorCount);
            _export.Insert(0, headerBuilder.ToString());
            
            base.WriteAssetToFile(fileName);
        }
    }
}