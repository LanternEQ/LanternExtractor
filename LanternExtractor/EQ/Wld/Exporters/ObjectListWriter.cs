using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ObjectListWriter : TextAssetWriter
    {
        public ObjectListWriter(int modelCount)
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Model List");
            _export.AppendLine("# Total models: " + modelCount);        
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            if (!(data is Mesh))
            {
                return;
            }

            _export.AppendLine(FragmentNameCleaner.CleanName(data));
        }
    }
}