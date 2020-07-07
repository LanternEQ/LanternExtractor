using System.Text;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ActorWriter : TextAssetWriter
    {
        private bool _isAnimatedActor;
        private int _actorCount;
        public ActorWriter(bool modelCount)
        {
            _isAnimatedActor = modelCount;
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            Actor actor = data as Actor;

            if (actor == null)
            {
                return;
            }
            
            if (_isAnimatedActor)
            {
                if (actor.SkeletonReference == null)
                {
                    return;
                }
                
                _export.AppendLine(FragmentNameCleaner.CleanName(actor));
                _actorCount++;
            }
            else
            {
                if (actor.MeshReference == null)
                {
                    return;
                }
                
                _export.AppendLine(FragmentNameCleaner.CleanName(actor));
                _actorCount++;
            }
        }

        public override void WriteAssetToFile(string fileName)
        {
            if (_export.Length == 0)
            {
                return;
            }
            
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.AppendLine(LanternStrings.ExportHeaderTitle + (_isAnimatedActor ? "Animated " : "Static ") +
                                     "Actor List");
            headerBuilder.AppendLine("# Total models: " + _actorCount);
            _export.Insert(0, headerBuilder.ToString());
            
            base.WriteAssetToFile(fileName);
        }
    }
}