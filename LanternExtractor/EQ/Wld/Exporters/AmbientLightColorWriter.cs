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
            DataTypes.Color ambientColor = new DataTypes.Color() { R = 0, B = 0, G = 0};
            var defaultLight = data as LightSource;
            var globalAmbientLight = data as GlobalAmbientLight;

            if (defaultLight == null && globalAmbientLight == null)
            {
                return;
            }

            if (defaultLight != null)
            {
                ambientColor = new DataTypes.Color()
                {
                    R = (int)defaultLight.Color.r,
                    G = (int)defaultLight.Color.g,
                    B = (int)defaultLight.Color.b
                };
            }
            else if (globalAmbientLight != null)
            {
                ambientColor = globalAmbientLight.Color;
            }

            _export.Append(ambientColor.R.ToString());
            _export.Append(",");
            _export.Append(ambientColor.G.ToString());
            _export.Append(",");
            _export.Append(ambientColor.B.ToString());
        }
    }
}
