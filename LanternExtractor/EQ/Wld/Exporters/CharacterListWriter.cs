using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class CharacterListWriter : TextAssetWriter
    {
        public CharacterListWriter(int modelCount)
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Character List");
            _export.AppendLine("# Total models: " + modelCount);
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            Actor model = data as Actor;
            
            if (model == null)
            {
                return;
            }
            
            _export.AppendLine(FragmentNameCleaner.CleanName(model));
        }
    }
}