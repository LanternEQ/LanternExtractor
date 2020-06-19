using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public static class MeshExporter
    {
        public static void ExportMeshes(WldFile wldFile, Settings _settings, ILogger logger)
        {
            List<WldFragment> meshFragments = wldFile.GetFragmentsOfType(FragmentType.Mesh);
            List<WldFragment> materialListFragments = wldFile.GetFragmentsOfType(FragmentType.MaterialList);
            
            if (meshFragments == null || meshFragments.Count == 0)
            {
                return;
            }
            
            string exportFolder = wldFile.GetExportFolderForWldType() + "Meshes/";
            
            TextAssetWriter meshWriter = null;
            TextAssetWriter collisionMeshWriter = null;
            TextAssetWriter materialListWriter = null;

            if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
            {
                meshWriter = new MeshIntermediateAssetWriter(_settings.ExportZoneMeshGroups, false);
                collisionMeshWriter = new MeshIntermediateAssetWriter(_settings.ExportZoneMeshGroups, true);
                materialListWriter = new MeshIntermediateMaterialsExport(_settings, wldFile.ZoneShortname, logger);
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
            
            // Exporting materials
            foreach (WldFragment fragment in materialListFragments)
            {
                materialListWriter.AddFragmentData(fragment);
                    
                var filePath = exportFolder + FragmentNameCleaner.CleanName(fragment)+ "_materials.txt";

                if (exportEachPass)
                {
                    if (_settings.ExportAllCharacterToSingleFolder && wldFile.WldType == WldType.Characters)
                    {
                        if (File.Exists(filePath))
                        {
                            var file = File.ReadAllText(filePath);
                            int oldFileSize = file.Length;
                            int newFileSize = materialListWriter.GetExportByteCount();
                            
                            if (newFileSize <= oldFileSize)
                            {
                                materialListWriter.ClearExportData();
                                continue;
                            }
                            
                        }
                    }

                    // TODO: Clean this up
                    (fragment as MaterialList).HasBeenExported = true;
                    materialListWriter.WriteAssetToFile(filePath);
                    materialListWriter.ClearExportData();
                }
            }

            if (!exportEachPass)
            {
                var filePath = exportFolder + wldFile.ZoneShortname + "_materials.txt";
                materialListWriter.WriteAssetToFile(filePath);
            }

            // Exporting meshes
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
                    // TODO: Fix this mess
                    if (wldFile.WldType == WldType.Characters && _settings.ExportAllCharacterToSingleFolder)
                    {
                        if (!((Mesh) fragment).MaterialList.HasBeenExported)
                        {
                            continue;
                        }
                    }
                    
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
        }
    }
}