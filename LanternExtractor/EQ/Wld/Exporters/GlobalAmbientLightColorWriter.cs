using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class GlobalAmbientLightColorWriter : TextAssetWriter
    {
        public GlobalAmbientLightColorWriter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Global Ambient Light Color");
            _export.AppendLine(LanternStrings.ExportHeaderFormat + "R, G, B");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            GlobalAmbientLightColor ambientLight = data as GlobalAmbientLightColor;

            if (ambientLight == null)
            {
                return;
            }

            _export.Append(ambientLight.Color.R.ToString());
            _export.Append(",");
            _export.Append(ambientLight.Color.G.ToString());
            _export.Append(",");
            _export.Append(ambientLight.Color.B.ToString());
        }
    }
}