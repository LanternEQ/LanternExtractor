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

            ObjectInstanceWriter writer = new ObjectInstanceWriter();

            foreach (WldFragment objectInstanceFragment in _fragmentTypeDictionary[FragmentType.ObjectInstance])
            {
                writer.AddFragmentData(objectInstanceFragment);
            }
            
            writer.WriteAssetToFile(zoneExportFolder + "object_instances.txt");
        }
        
        private void ExportObjectVertexColors()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.ObjectInstance))
            {
                _logger.LogWarning("Cannot export vertex colors. No object instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/Objects/VertexColors/";

            VertexColorsWriter writer = new VertexColorsWriter();

            foreach (WldFragment objectInstanceFragment in _fragmentTypeDictionary[FragmentType.ObjectInstance])
            {
                VertexColors vertexColors = (objectInstanceFragment as ObjectInstance)?.Colors;

                if (vertexColors == null)
                {
                    continue;
                }
                
                writer.AddFragmentData(vertexColors);
                writer.WriteAssetToFile(zoneExportFolder + "vc_" + vertexColors.Index + ".txt");
                writer.ClearExportData();
            }
        }
    }
}