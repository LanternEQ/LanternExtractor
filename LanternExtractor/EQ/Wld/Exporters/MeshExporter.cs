using System.Collections.Generic;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public static class MeshExporter
    {
        public static void ExportMeshes(WldFile wldFile, Settings _settings)
        {
            List<WldFragment> meshFragments = wldFile.GetFragmentsOfType(FragmentType.Mesh);
            List<WldFragment> materialListFragments = wldFile.GetFragmentsOfType(FragmentType.MaterialList);
            
            if (meshFragments == null || meshFragments.Count == 0)
            {
                return;
            }
            
            string exportFolder = wldFile.GetExportFolderForWldType();
            
            TextAssetWriter meshWriter = null;
            TextAssetWriter collisionMeshWriter = null;
            TextAssetWriter materialListWriter = null;

            if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
            {
                meshWriter = new MeshIntermediateAssetWriter(_settings.ExportZoneMeshGroups, false);
                collisionMeshWriter = new MeshIntermediateAssetWriter(_settings.ExportZoneMeshGroups, true);
                materialListWriter = new MeshIntermediateMaterialsExport(_settings, wldFile.ZoneShortname);
            }

            bool exportCollisionMesh = false;
            bool exportEachPass = wldFile.WldType != WldType.Zone || _settings.ExportZoneMeshGroups;

            // If it's a zone mesh, we need to ensure we should export a collision mesh.
            // For objects, it's done for each fragment
            if (!exportEachPass)
            {
                foreach (WldFragment fragment in meshFragments)
                {
                    Mesh mesh = fragment as Mesh;

                    if (mesh == null)
                    {
                        continue;
                    }

                    if (!mesh.ExportSeparateCollision)
                    {
                        continue;
                    }
                    
                    exportCollisionMesh = true;
                    break;
                }
            }

            foreach (WldFragment fragment in meshFragments)
            {
                meshWriter.AddFragmentData(fragment);

                // Determine if we need collision
                if (exportEachPass)
                {
                    Mesh mesh = fragment as Mesh;
                    exportCollisionMesh = mesh != null && mesh.ExportSeparateCollision;
                }

                if (exportCollisionMesh)
                {
                    collisionMeshWriter.AddFragmentData(fragment);
                }

                if (exportEachPass)
                {
                    meshWriter.WriteAssetToFile(exportFolder + FragmentNameCleaner.CleanName(fragment)+ ".txt");
                    meshWriter.ClearExportData();

                    if (exportCollisionMesh)
                    {
                        collisionMeshWriter.WriteAssetToFile(exportFolder + FragmentNameCleaner.CleanName(fragment)+ "_collision.txt");
                        collisionMeshWriter.ClearExportData();
                    }
                }
            }

            if (!exportEachPass)
            {
                meshWriter.WriteAssetToFile(exportFolder + wldFile.ZoneShortname + ".txt");

                if (exportCollisionMesh)
                {
                    collisionMeshWriter.WriteAssetToFile(exportFolder + wldFile.ZoneShortname + "_collision.txt");
                }
            }
            
            foreach (WldFragment fragment in materialListFragments)
            {
                materialListWriter.AddFragmentData(fragment);
                    
                if (exportEachPass)
                {
                    materialListWriter.WriteAssetToFile(exportFolder + FragmentNameCleaner.CleanName(fragment)+ "_materials.txt");
                    materialListWriter.ClearExportData();
                }
            }

            if (!exportEachPass)
            {
                materialListWriter.WriteAssetToFile(exportFolder + wldFile.ZoneShortname + "_materials.txt");
            }

            /*
            bool useGroups = _settings.ExportZoneMeshGroups;

            if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
            {
                MeshIntermediateAssetWriter assetWriter = new MeshIntermediateAssetWriter(useGroups, false);
                MeshIntermediateAssetWriter collisionAssetWriter = new MeshIntermediateAssetWriter(useGroups, true);

                foreach (WldFragment fragment in meshFragments)
                {
                    assetWriter.AddFragmentData(fragment);
                    collisionAssetWriter.AddFragmentData(fragment);

                    if (useGroups)
                    {
                        assetWriter.WriteAssetToFile(zoneExportFolder + FragmentNameCleaner.CleanName(fragment)+ ".txt");
                        assetWriter.ClearExportData();
                        
                        collisionAssetWriter.WriteAssetToFile(zoneExportFolder + FragmentNameCleaner.CleanName(fragment)+ "_collision.txt");
                        collisionAssetWriter.ClearExportData();
                    }
                }

                if (!useGroups)
                {
                    assetWriter.WriteAssetToFile(zoneExportFolder + wldFile.ZoneShortname + ".txt");
                    collisionAssetWriter.WriteAssetToFile(zoneExportFolder + wldFile.ZoneShortname + "_collision.txt");
                }

                MeshIntermediateMaterialsExport mtlExporter = new MeshIntermediateMaterialsExport(_settings, wldFile.ZoneShortname);

                foreach (WldFragment fragment in materialListFragments)
                {
                    mtlExporter.AddFragmentData(fragment);
                    
                    if (useGroups)
                    {
                        mtlExporter.WriteAssetToFile(zoneExportFolder + FragmentNameCleaner.CleanName(fragment)+ "_materials.txt");
                        mtlExporter.ClearExportData();
                    }
                }

                if (!useGroups)
                {
                    mtlExporter.WriteAssetToFile(zoneExportFolder + wldFile.ZoneShortname + "_materials.txt");
                }
            }
            else if (_settings.ModelExportFormat == ModelExportFormat.Obj)
            {
                MeshObjWriter writer = new MeshObjWriter(ObjExportType.Textured, false, true, "sky", "sky");

                foreach (WldFragment fragment in meshFragments)
                {
                    writer.AddFragmentData(fragment);
                }
            
                writer.WriteAssetToFile(zoneExportFolder + wldFile.ZoneShortname + LanternStrings.ObjFormatExtension);
            
                MeshObjMtlWriter mtlWriter = new MeshObjMtlWriter(_settings, wldFile.ZoneShortname);

                foreach (WldFragment fragment in materialListFragments)
                {
                    mtlWriter.AddFragmentData(fragment);
                }
            
                mtlWriter.WriteAssetToFile(zoneExportFolder + wldFile.ZoneShortname + LanternStrings.FormatMtlExtension);
            }*/
        }
    }
}