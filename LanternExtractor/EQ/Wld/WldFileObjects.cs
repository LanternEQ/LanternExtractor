using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileObjects : WldFile
    {
        public WldFileObjects(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings, WldFile wldToInject = null) : base(
            wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }
        
        protected override void ExportData()
        {
            ExportZoneObjectData();
            ExportMaterialList();
        }
        
         /// <summary>
        /// Export zone object meshes to .obj files and collision meshes if there are non-solid polygons
        /// Additionally, it exports a list of vertex animated instances
        /// </summary>
        private void ExportZoneObjectData()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                _logger.LogWarning("Cannot export zone object meshes. No meshes found.");
                return;
            }
            
            ObjectListExporter objectListExporter = new ObjectListExporter(_fragmentTypeDictionary[FragmentType.Mesh].Count);
            
            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
            string rootExportFolder = _zoneName + "/";
            
            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                objectListExporter.AddFragmentData(fragment);
                
                string meshName = FragmentNameCleaner.CleanName(fragment);

                if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
                {
                    MeshIntermediateExporter meshExporter = new MeshIntermediateExporter();
                    meshExporter.AddFragmentData(fragment);
                    meshExporter.WriteAssetToFile(objectsExportFolder + meshName + ".txt");
                }
                else if (_settings.ModelExportFormat == ModelExportFormat.Obj)
                {
                    MeshObjExporter meshExporter = new MeshObjExporter(ObjExportType.Textured, _settings.ExportHiddenGeometry, false, meshName);
                    MeshObjExporter collisionMeshExport = new MeshObjExporter(ObjExportType.Collision, _settings.ExportHiddenGeometry, false, meshName);
                    meshExporter.AddFragmentData(fragment);
                    collisionMeshExport.AddFragmentData(fragment);

                    meshExporter.WriteAssetToFile(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                    meshExporter.WriteAllFrames(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                    meshExporter.WriteAssetToFile(objectsExportFolder + meshName + "_collision" + LanternStrings.ObjFormatExtension);
                }
            }
            
            objectListExporter.WriteAssetToFile(rootExportFolder + _zoneName + "_objects.txt");
            
            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                string listName = FragmentNameCleaner.CleanName(fragment);

                if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
                {
                    MeshIntermediateMaterialsExport mtlExporter = new MeshIntermediateMaterialsExport(_settings, _zoneName);
                    mtlExporter.AddFragmentData(fragment);
                    mtlExporter.WriteAssetToFile(objectsExportFolder + listName + "_materials.txt");
                }
                else
                {
                    MeshObjMtlExporter mtlExporter = new MeshObjMtlExporter(_settings, _zoneName);
                    mtlExporter.AddFragmentData(fragment);
                    mtlExporter.WriteAssetToFile(objectsExportFolder + listName + LanternStrings.FormatMtlExtension);
                }
            }
        } 
    }
}