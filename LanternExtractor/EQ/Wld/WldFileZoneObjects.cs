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
            ExportObjectVertexColors();
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
            
            exporter.WriteAssetToFile(zoneExportFolder + _zoneName + "_object_instances.txt");
        }
        
        private void ExportObjectVertexColors()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.ObjectInstance))
            {
                _logger.LogWarning("Cannot export vertex colors. No object instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/Objects/VertexColors/";

            VertexColorsExporter exporter = new VertexColorsExporter();

            foreach (WldFragment objectInstanceFragment in _fragmentTypeDictionary[FragmentType.ObjectInstance])
            {
                VertexColors vertexColors = (objectInstanceFragment as ObjectInstance)?.Colors;

                if (vertexColors == null)
                {
                    continue;
                }
                
                exporter.AddFragmentData(vertexColors);
                exporter.WriteAssetToFile(zoneExportFolder + "vc_" + vertexColors.Index + ".txt");
                exporter.ClearExportData();
            }
        }
    }
}