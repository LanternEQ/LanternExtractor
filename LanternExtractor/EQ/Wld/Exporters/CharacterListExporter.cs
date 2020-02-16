using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class CharacterListExporter : TextAssetExporter
    {
        public CharacterListExporter(string zoneName, int modelCount)
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Character List");
            _export.AppendLine("# Total models: " + modelCount);
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            ModelReference model = data as ModelReference;
            
            if (model == null)
            {
                return;
            }
            
            _export.AppendLine(FragmentNameCleaner.CleanName(model));
        }
    }
}