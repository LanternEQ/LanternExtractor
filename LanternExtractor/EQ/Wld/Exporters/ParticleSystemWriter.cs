using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ParticleSystemWriter : TextAssetWriter
    {
        public ParticleSystemWriter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Particle System");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            if (!(data is ParticleCloud))
            {
                return;
            }

            _export.AppendLine(FragmentNameCleaner.CleanName(data));
        }
    }
}