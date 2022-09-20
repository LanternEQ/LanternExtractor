using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class AmbientLightColorWriter : TextAssetWriter
    {
        public AmbientLightColorWriter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Ambient Light Color");
            _export.AppendLine(LanternStrings.ExportHeaderFormat + "R, G, B");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            GlobalAmbientLight globalAmbientLight = data as GlobalAmbientLight;

            if (globalAmbientLight == null)
            {
                return;
            }

            _export.Append(globalAmbientLight.Color.R.ToString());
            _export.Append(",");
            _export.Append(globalAmbientLight.Color.G.ToString());
            _export.Append(",");
            _export.Append(globalAmbientLight.Color.B.ToString());
        }
    }
}