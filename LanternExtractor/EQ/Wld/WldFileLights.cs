using System.Globalization;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileLights : WldFile
    {
        public WldFileLights(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings) : base(wldFile, zoneName, type, logger, settings)
        {
        }
        
        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportWldData()
        {
            ExportLightInstanceList();
        }

        
        /// <summary>
        /// Exports the list of light instances (contains position, colors, radius)
        /// </summary>
        private void ExportLightInstanceList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x28))
            {
                _logger.LogWarning("Unable to export light instance list. No instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/";

            Directory.CreateDirectory(zoneExportFolder);

            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};

            var lightListExport = new StringBuilder();

            lightListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Light Instances");
            lightListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                       "PosX, PosY, PosZ, Radius, ColorR, ColorG, ColorB");

            for (int i = 0; i < _fragmentTypeDictionary[0x28].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x28][i] is LightInfo lightInfo))
                {
                    continue;
                }

                lightListExport.Append(lightInfo.Position.x.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.Position.z.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.Position.y.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.Radius.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.LightReference.LightSource.Color.r.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.LightReference.LightSource.Color.g.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.LightReference.LightSource.Color.b.ToString(format));
                lightListExport.AppendLine();
            }

            File.WriteAllText(zoneExportFolder + _zoneName + "_lights.txt", lightListExport.ToString());
        }
    }
}