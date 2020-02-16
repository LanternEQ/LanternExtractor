using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileLights : WldFile
    {
        public WldFileLights(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportData()
        {
            ExportLightInstanceList();
        }
        
        /// <summary>
        /// Exports the list of light instances (contains position, colors, radius)
        /// </summary>
        private void ExportLightInstanceList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.LightInstance))
            {
                _logger.LogWarning("Unable to export light instance list. No instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/";
            
            LightInstancesExporter exporter = new LightInstancesExporter();

            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.LightInstance])
            {
                exporter.AddFragmentData(fragment);
            }
            
            exporter.WriteAssetToFile(zoneExportFolder + _zoneName + "_lights.txt");
        }
    }
}