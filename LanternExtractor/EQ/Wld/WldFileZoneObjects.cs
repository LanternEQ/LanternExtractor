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
            ExportObjectInstanceAndVertexColorList();
        }
        
        private void ExportObjectInstanceAndVertexColorList()
        {
            var instanceList = GetFragmentsOfType2<ObjectInstance>();
            
            if (instanceList.Count == 0)
            {
                _logger.LogWarning("Cannot export object instance list. No object instances found.");
                return;
            }
            
            ObjectInstanceWriter instanceWriter = new ObjectInstanceWriter();
            VertexColorsWriter colorWriter = new VertexColorsWriter();
            
            string colorsExportFolder = GetRootExportFolder() + "Objects/VertexColors/";

            foreach (var instance in instanceList)
            {
                instanceWriter.AddFragmentData(instance);
                
                if (instance.Colors == null)
                {
                    continue;
                }
                
                colorWriter.AddFragmentData(instance.Colors);
                colorWriter.WriteAssetToFile(colorsExportFolder + "vc_" + instance.Index + ".txt");
                colorWriter.ClearExportData();
            }
            
            colorWriter.WriteAssetToFile(GetRootExportFolder() + "object_instances.txt");
        }
    }
}