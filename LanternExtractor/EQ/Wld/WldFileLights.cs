using LanternExtractor.EQ.Archive;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Settings;
using Serilog;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileLights : WldFile
    {
        public WldFileLights(ArchiveFile wldFile, string zoneName, WldType type, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, settings, wldToInject)
        {
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        public override void ExportData()
        {
            ExportLightInstanceList();
        }

        /// <summary>
        /// Exports the list of light instances (position, colors, radius)
        /// </summary>
        private void ExportLightInstanceList()
        {
            var lightInstances = GetFragmentsOfType<LightInstance>();

            if (lightInstances.Count == 0)
            {
                Log.Warning("Unable to export light instance list. No instances found.");
                return;
            }

            var writer = new LightInstancesWriter();
            foreach (var light in lightInstances)
            {
                writer.AddFragmentData(light);
            }

            writer.WriteAssetToFile(GetExportFolderForWldType() + "light_instances.txt");
        }
    }
}
