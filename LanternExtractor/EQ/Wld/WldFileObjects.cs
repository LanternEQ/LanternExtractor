using LanternExtractor.EQ.Pfs;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileObjects : WldFile
    {
        public WldFileObjects(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(
            wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }

        protected override void ExportData()
        {
            base.ExportData();
            ExportMeshList();
        }
    }
}