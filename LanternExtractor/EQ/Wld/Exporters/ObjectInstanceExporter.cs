using System.Globalization;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ObjectInstanceExporter : TextAssetExporter
    {
        public ObjectInstanceExporter()
        {
            
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Object Instances");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                                        "ModelName, PosX, PosY, PosZ, RotX, RotY, RotZ, ScaleX, ScaleY, ScaleZ");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            ObjectInstance instance = data as ObjectInstance;

            if (instance == null)
            {
                return;
            }
            
            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};
            
            _export.Append(instance.ObjectName);
            _export.Append(",");
            _export.Append(instance.Position.x.ToString(format));
            _export.Append(",");
            _export.Append(instance.Position.z.ToString(format));
            _export.Append(",");
            _export.Append(instance.Position.y.ToString(format));
            _export.Append(",");
            _export.Append(instance.Rotation.x.ToString(format));
            _export.Append(",");
            _export.Append(instance.Rotation.z.ToString(format));
            _export.Append(",");
            _export.Append(instance.Rotation.y.ToString(format));
            _export.Append(",");
            _export.Append(instance.Scale.x.ToString(format));
            _export.Append(",");
            _export.Append(instance.Scale.y.ToString(format));
            _export.Append(",");
            _export.Append(instance.Scale.z.ToString(format));
            _export.AppendLine();
            
        }        
    }
}