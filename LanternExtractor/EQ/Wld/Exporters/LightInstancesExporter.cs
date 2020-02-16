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

            _export.Append(light.Position.x.ToString(format));
            _export.Append(",");
            _export.Append(light.Position.z.ToString(format));
            _export.Append(",");
            _export.Append(light.Position.y.ToString(format));
            _export.Append(",");
            _export.Append(light.Radius.ToString(format));
            _export.Append(",");
            _export.Append(light.LightReference.LightSource.Color.r.ToString(format));
            _export.Append(",");
            _export.Append(light.LightReference.LightSource.Color.g.ToString(format));
            _export.Append(",");
            _export.Append(light.LightReference.LightSource.Color.b.ToString(format));
            _export.AppendLine();
        }
    }
}