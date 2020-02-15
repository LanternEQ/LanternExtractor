using System.IO;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileModels : WldFile
    {
        public WldFileModels(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToIbject = null) : base(wldFile, zoneName, type, logger, settings, wldToIbject)
        {
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportWldData()
        {
            base.ExportWldData();
            ExportModels();
            ExportMaterialList();
        }

        private void ExportModels()
        {
            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportModelsFolder;

            Directory.CreateDirectory(objectsExportFolder);
            
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                return;
            }

            foreach (WldFragment model in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                Mesh mesh = model as Mesh;

                if (mesh == null)
                {
                    continue;
                }

                int vertexCount;
                Material lastMaterialUsed = null;

                var meshString = mesh.GetMeshExport(0, null, ObjExportType.Textured, out vertexCount, out lastMaterialUsed, _settings, _logger);
                File.WriteAllText(objectsExportFolder + "/" + model.Name + ".obj", meshString[0]);
            }
        }
    }
}