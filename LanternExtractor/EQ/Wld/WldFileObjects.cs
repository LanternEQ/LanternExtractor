using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
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

            
            foreach (WldFragment listFragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                string meshName = listFragment.Name.Replace("_DMSPRITEDEF", "").ToLower();
                
                MeshObjExporter meshExporter = new MeshObjExporter(ObjExportType.Textured, _settings.ExportHiddenGeometry, meshName);
                MeshObjExporter collisionMeshExport = new MeshObjExporter(ObjExportType.Collision, _settings.ExportHiddenGeometry, meshName);
                meshExporter.AddFragmentData(listFragment);
                collisionMeshExport.AddFragmentData(listFragment);

                meshExporter.WriteAssetToFile(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                meshExporter.WriteAllFrames(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                meshExporter.WriteAssetToFile(objectsExportFolder + meshName + "_collision" + LanternStrings.ObjFormatExtension);
            }
            
            foreach (WldFragment listFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                string listName = listFragment.Name.Replace("_MP", "").ToLower();

                MeshObjMtlExporter mtlExporter = new MeshObjMtlExporter(_settings, _zoneName);
                mtlExporter.AddFragmentData(listFragment);
                mtlExporter.WriteAssetToFile(objectsExportFolder + listName + LanternStrings.FormatMtlExtension);
            }
        } 
    }
}