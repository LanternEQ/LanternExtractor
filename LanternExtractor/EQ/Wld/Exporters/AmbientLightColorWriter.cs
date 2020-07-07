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
            AmbientLightColor ambientLight = data as AmbientLightColor;

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