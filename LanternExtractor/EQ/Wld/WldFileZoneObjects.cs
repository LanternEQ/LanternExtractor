using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileZoneObjects : WldFile
    {
        public WldFileZoneObjects(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings, WldFile wldToInject = null) : base(
            wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }
        
        protected override void ExportData()
        {
            ExportObjectInstanceList();
        }
        
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