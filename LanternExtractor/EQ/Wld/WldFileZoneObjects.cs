using System.Globalization;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileZoneObjects : WldFile
    {
        public WldFileZoneObjects(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings) : base(
            wldFile, zoneName, type, logger, settings)
        {
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportWldData()
        {
            ExportObjectInstanceList();
        }
        
        /// <summary>
        /// Exports the list of objects instances
        /// This includes information about position, rotation, and scaling
        /// </summary>
        private void ExportObjectInstanceList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x15))
            {
                _logger.LogWarning("Cannot export object instance list. No object instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/";

            Directory.CreateDirectory(zoneExportFolder);

            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};

            var objectListExport = new StringBuilder();

            objectListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Instances");
            objectListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                        "ModelName, PosX, PosY, PosZ, RotX, RotY, RotZ, ScaleX, ScaleY, ScaleZ");

            for (int i = 0; i < _fragmentTypeDictionary[0x15].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x15][i] is ObjectInstance objectLocation))
                {
                    continue;
                }

                objectListExport.Append(objectLocation.ObjectName);
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Position.x.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Position.z.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Position.y.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Rotation.x.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Rotation.z.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Rotation.y.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Scale.x.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Scale.y.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Scale.z.ToString(format));
                objectListExport.AppendLine();
            }

            File.WriteAllText(zoneExportFolder + _zoneName + "_objects.txt", objectListExport.ToString());
        }
    }
}