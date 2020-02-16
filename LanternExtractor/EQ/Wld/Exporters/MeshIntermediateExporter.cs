using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshIntermediateExporter : TextAssetExporter
    {
        public MeshIntermediateExporter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Intermediate Mesh");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            
        }
    }
}