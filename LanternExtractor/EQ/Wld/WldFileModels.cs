using System.IO;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileModels : WldFile
    {
        public WldFileModels(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }
        
        protected override void ExportData()
        {
            base.ExportData();
            ExportModels();
        }

        private void ExportModels()
        {
            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportModelsFolder;

            Directory.CreateDirectory(objectsExportFolder);
            
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                return;
            }

            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                string meshName = FragmentNameCleaner.CleanName(fragment);
                MeshObjWriter writer = new MeshObjWriter(ObjExportType.Textured, false, false, meshName);
                writer.AddFragmentData(fragment);
                writer.WriteAssetToFile(objectsExportFolder + "/" + meshName + ".obj");
            }
            
            foreach (WldFragment listFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                string listName = FragmentNameCleaner.CleanName(listFragment);
                MeshObjMtlWriter mtlWriter = new MeshObjMtlWriter(_settings, _zoneName);
                mtlWriter.AddFragmentData(listFragment);
                mtlWriter.WriteAssetToFile(objectsExportFolder + "/" + listName + LanternStrings.FormatMtlExtension);
            }
        }
    }
}