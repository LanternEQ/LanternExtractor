using System.Text;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ActorWriterNew : TextAssetWriter
    {
        public override void AddFragmentData(WldFragment data)
        {
            Actor actor = data as Actor;

            if (actor == null)
            {
                return;
            }

            _export.Append(actor.ActorType.ToString());
            _export.Append(",");
            _export.Append(actor.ReferenceName);
            
            _export.Append(FragmentNameCleaner.CleanName(actor));
            _export.AppendLine();
        }

        public override void WriteAssetToFile(string fileName)
        {
            if (_export.Length == 0)
            {
                return;
            }
            
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.AppendLine(LanternStrings.ExportHeaderTitle + "Actor");
            _export.Insert(0, headerBuilder.ToString());
            base.WriteAssetToFile(fileName);
        }
    }
}