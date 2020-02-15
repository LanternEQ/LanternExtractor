using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileZoneObjects : WldFile
    {
        public WldFileZoneObjects(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings, WldFile wldToIbject = null) : base(
            wldFile, zoneName, type, logger, settings, wldToIbject)
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
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.ObjectInstance))
            {
                _logger.LogWarning("Cannot export object instance list. No object instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/";

            ObjectInstanceExporter exporter = new ObjectInstanceExporter();

            foreach (WldFragment objectInstanceFragment in _fragmentTypeDictionary[FragmentType.ObjectInstance])
            {
                exporter.AddFragmentData(objectInstanceFragment);
            }
            
            exporter.WriteAssetToFile(_zoneName + "/" + _zoneName + "_objects.txt");
        }
    }
}