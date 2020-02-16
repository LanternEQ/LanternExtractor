using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class LightInstancesExporter : TextAssetExporter
    {
        public LightInstancesExporter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Light Instances");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                                       "PosX, PosY, PosZ, Radius, ColorR, ColorG, ColorB");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            LightInstance light = data as LightInstance;

            if (light == null)
            {
                return;
            }

            _export.Append(light.Position.x.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(light.Position.z.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(light.Position.y.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(light.Radius.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(light.LightReference.LightSource.Color.r.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(light.LightReference.LightSource.Color.g.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(light.LightReference.LightSource.Color.b.ToString(_numberFormat));
            _export.AppendLine();
        }
    }
}