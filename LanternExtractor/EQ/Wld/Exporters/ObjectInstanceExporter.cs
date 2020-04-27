using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ObjectInstanceExporter : TextAssetExporter
    {
        public ObjectInstanceExporter()
        {
            
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Object Instances");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                                        "ModelName, PosX, PosY, PosZ, RotX, RotY, RotZ, ScaleX, ScaleY, ScaleZ, ColorIndex");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            ObjectInstance instance = data as ObjectInstance;

            if (instance == null)
            {
                return;
            }
            
            _export.Append(instance.ObjectName);
            _export.Append(",");
            _export.Append(instance.Position.x.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Position.z.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Position.y.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Rotation.x.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Rotation.z.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Rotation.y.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Scale.x.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Scale.y.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Scale.z.ToString(_numberFormat));
            _export.Append(",");
            _export.Append(instance.Colors == null ? -1 :instance.Colors.Index);

            _export.AppendLine();
        }        
    }
}