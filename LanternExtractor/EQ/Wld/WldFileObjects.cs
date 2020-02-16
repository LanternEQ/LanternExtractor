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
            ExportZoneObjectMeshes();
            ExportMaterialList();
        }
        
         /// <summary>
        /// Export zone object meshes to .obj files and collision meshes if there are non-solid polygons
        /// Additionally, it exports a list of vertex animated instances
        /// </summary>
        private void ExportZoneObjectMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                _logger.LogWarning("Cannot export zone object meshes. No meshes found.");
                return;
            }
            
            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
            
            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                string meshName = FragmentNameCleaner.CleanName(fragment);
                
                MeshObjExporter meshExporter = new MeshObjExporter(ObjExportType.Textured, _settings.ExportHiddenGeometry, meshName);
                MeshObjExporter collisionMeshExport = new MeshObjExporter(ObjExportType.Collision, _settings.ExportHiddenGeometry, meshName);
                meshExporter.AddFragmentData(fragment);
                collisionMeshExport.AddFragmentData(fragment);

                meshExporter.WriteAssetToFile(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                meshExporter.WriteAllFrames(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                meshExporter.WriteAssetToFile(objectsExportFolder + meshName + "_collision" + LanternStrings.ObjFormatExtension);
            }
            
            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                string listName = FragmentNameCleaner.CleanName(fragment);
                MeshObjMtlExporter mtlExporter = new MeshObjMtlExporter(_settings, _zoneName);
                mtlExporter.AddFragmentData(fragment);
                mtlExporter.WriteAssetToFile(objectsExportFolder + listName + LanternStrings.FormatMtlExtension);
            }
        } 
    }
}