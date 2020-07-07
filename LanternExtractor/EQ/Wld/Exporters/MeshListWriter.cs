using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshListWriter : TextAssetWriter
    {
        public override void AddFragmentData(WldFragment data)
        {
            Mesh mesh = data as Mesh;

            if (mesh == null)
            {
                return;
            }

            _export.AppendLine(FragmentNameCleaner.CleanName(mesh).ToLower());
        }
    }
}