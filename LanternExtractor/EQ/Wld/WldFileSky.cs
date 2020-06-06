using System.IO;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileSky : WldFile
    {
        public WldFileSky(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportData()
        {

            base.ExportData();
            ExportSkyMeshList();
        }

        private void ExportSkyMeshList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                return;
            }
            
            MeshListWriter meshListWriter = new MeshListWriter();

            foreach (var fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                Mesh meshFragment = fragment as Mesh;

                if (meshFragment == null)
                {
                    continue;
                }
                
                meshListWriter.AddFragmentData(meshFragment);
            }
            
            string meshListPath = GetExportFolderForWldType() + "meshes.txt";
            
            meshListWriter.WriteAssetToFile(meshListPath);
        }
    }
}