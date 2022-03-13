﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public static class MeshExporter
    {
        public static void ExportMeshes(WldFile wldFile, Settings settings, ILogger logger)
        {
            List<Mesh> meshes = wldFile.GetFragmentsOfType<Mesh>();
            List<MaterialList> materialLists = wldFile.GetFragmentsOfType<MaterialList>();
            List<LegacyMesh> legacyMeshes = wldFile.GetFragmentsOfType<LegacyMesh>();

            if (meshes.Count == 0 && legacyMeshes.Count == 0)
            {
                return;
            }

            string exportFolder = wldFile.GetExportFolderForWldType() + "/";
            
            var meshWriter = new MeshIntermediateAssetWriter(settings.ExportZoneMeshGroups, false);
            var legacyMeshWriter = new LegacyMeshIntermediateAssetWriter();
            var collisionMeshWriter = new MeshIntermediateAssetWriter(settings.ExportZoneMeshGroups, true);
            var materialListWriter = new MeshIntermediateMaterialsWriter();

            bool exportEachPass = wldFile.WldType != WldType.Zone || settings.ExportZoneMeshGroups;

            // If it's a zone mesh, we need to ensure we should export a collision mesh.
            // There are some zones with no non solid polygons (e.g. arena). No collision mesh is exported in this case.
            // For objects, it's done for each fragment
            bool exportCollisionMesh = !exportEachPass && meshes.Where(m => m.ExportSeparateCollision).Any();
            /*if (!exportEachPass)
            {
                foreach (Mesh mesh in meshes)
                {
                    if (!mesh.ExportSeparateCollision)
                    {
                        continue;
                    }

                    exportCollisionMesh = true;
                    break;
                }

                if (legacyMeshes != null)
                {
                    foreach (var alternateMesh in legacyMeshes)
                    {
                        // if (!mesh.ExportSeparateCollision)
                        // {
                        //     continue;
                        // }

                        exportCollisionMesh = true;
                        break;
                    }
                }
            }*/

            // Export materials
            if (materialLists != null)
            {
                foreach (MaterialList materialList in materialLists)
                {
                    materialListWriter.AddFragmentData(materialList);

                    var newExportFolder = wldFile.GetExportFolderForWldType() + "/MaterialLists/";
                    var filePath = newExportFolder + FragmentNameCleaner.CleanName(materialList) +
                                   ".txt";

                    if (!exportEachPass)
                    {
                        continue;
                    }
                    
                    if (settings.ExportCharactersToSingleFolder && wldFile.WldType == WldType.Characters)
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
                    
                    materialList.HasBeenExported = true;
                    materialListWriter.WriteAssetToFile(filePath);
                    materialListWriter.ClearExportData();
                }
            }


            if (!exportEachPass)
            {
                var filePath = wldFile.GetExportFolderForWldType() + "/MaterialLists/" + wldFile.ZoneShortname +
                               ".txt";
                materialListWriter.WriteAssetToFile(filePath);
            }

            // Exporting meshes
            foreach (Mesh mesh in meshes)
            {
                if (mesh == null)
                {
                    continue;
                }
                
                meshWriter.AddFragmentData(mesh);

                // Determine if we need collision
                if (exportEachPass)
                {
                    exportCollisionMesh = mesh.ExportSeparateCollision;
                }

                if (exportCollisionMesh)
                {
                    collisionMeshWriter.AddFragmentData(mesh);
                }

                if (exportEachPass)
                {
                    // TODO: Fix this mess
                    if (wldFile.WldType == WldType.Characters && settings.ExportCharactersToSingleFolder)
                    {
                        if (!mesh.MaterialList.HasBeenExported)
                        {
                            meshWriter.ClearExportData();
                            collisionMeshWriter.ClearExportData();
                            continue;
                        }
                    }

                    meshWriter.WriteAssetToFile(exportFolder + "Meshes/" + FragmentNameCleaner.CleanName(mesh) +
                                                ".txt");
                    meshWriter.ClearExportData();

                    if (exportCollisionMesh)
                    {
                        collisionMeshWriter.WriteAssetToFile(exportFolder + "Meshes/" +
                                                             FragmentNameCleaner.CleanName(mesh) +
                                                             "_collision" +
                                                             ".txt");
                        collisionMeshWriter.ClearExportData();
                    }
                }
            }

            if (legacyMeshes != null)
            {
                foreach (var alternateMesh in legacyMeshes)
                {
                    legacyMeshWriter.AddFragmentData(alternateMesh);
                    
                    if (exportEachPass)
                    {
                        var newExportFolder = wldFile.GetExportFolderForWldType() + "/AlternateMeshes/";
                        Directory.CreateDirectory(newExportFolder);
                        legacyMeshWriter.WriteAssetToFile(newExportFolder +
                                                             FragmentNameCleaner.CleanName(alternateMesh) +
                                                             ".txt");
                        legacyMeshWriter.ClearExportData();
                    }
                }
            }
            

            if (!exportEachPass)
            {
                meshWriter.WriteAssetToFile(exportFolder + "Meshes/" + wldFile.ZoneShortname +
                                            ".txt");

                if (exportCollisionMesh)
                {
                    collisionMeshWriter.WriteAssetToFile(exportFolder + "Meshes/" + wldFile.ZoneShortname + "_collision" +
                                                                      ".txt");
                }
            }
        }
    }
}