using System;
using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public static class MeshExporter
    {
        public static void ExportMeshes(WldFile wldFile, Settings settings, ILogger logger)
        {
            List<WldFragment> meshFragments = wldFile.GetFragmentsOfType(FragmentType.Mesh);
            List<AlternateMesh> alternateMeshFragments = wldFile.GetFragmentsOfType2<AlternateMesh>();
            List<WldFragment> materialListFragments = wldFile.GetFragmentsOfType(FragmentType.MaterialList);
            
            if (meshFragments?.Count == 0 && alternateMeshFragments?.Count == 0)
            {
                return;
            }

            string exportFolder = GetExportFolderForExportFormat(wldFile.GetExportFolderForWldType(), settings.ModelExportFormat);
            
            TextAssetWriter meshWriter;
            TextAssetWriter alternateMeshWriter = null;
            TextAssetWriter collisionMeshWriter;
            TextAssetWriter materialListWriter;

            switch (settings.ModelExportFormat)
            {
                case ModelExportFormat.Intermediate:
                {
                    meshWriter = new MeshIntermediateAssetWriter(settings.ExportZoneMeshGroups, false);
                    alternateMeshWriter = new AlternateMeshIntermediateAssetWriter(settings.ExportZoneMeshGroups, true);
                    collisionMeshWriter = new MeshIntermediateAssetWriter(settings.ExportZoneMeshGroups, true);
                    materialListWriter = new MeshIntermediateMaterialsExport(settings, wldFile.ZoneShortname, logger);
                    break;
                }
                case ModelExportFormat.Obj:
                {
                    meshWriter = new MeshObjWriter(ObjExportType.Textured, settings.ExportHiddenGeometry, settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
                    collisionMeshWriter = new MeshObjWriter(ObjExportType.Collision, settings.ExportHiddenGeometry, settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
                    materialListWriter = new MeshObjMtlWriter(settings, wldFile.ZoneShortname);
                    break;
                }
                default:
                {
                    return;
                }
            }

            bool exportCollisionMesh = false;
            bool exportEachPass = wldFile.WldType != WldType.Zone || settings.ExportZoneMeshGroups;

            // If it's a zone mesh, we need to ensure we should export a collision mesh.
            // There are some zones with no non solid polygons (e.g. arena). No collision mesh is exported in this case.
            // For objects, it's done for each fragment
            if (!exportEachPass)
            {
                if (meshFragments != null)
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

                if (alternateMeshFragments != null)
                {
                    foreach (var alternateMesh in alternateMeshFragments)
                    {
                        // if (!mesh.ExportSeparateCollision)
                        // {
                        //     continue;
                        // }
                    
                        exportCollisionMesh = true;
                        break;
                    }
                }
            }
            
            // Export materials
            if (materialListFragments != null)
            {
                foreach (WldFragment fragment in materialListFragments)
                {
                    materialListWriter.AddFragmentData(fragment);

                    var filePath = exportFolder + FragmentNameCleaner.CleanName(fragment) + "_materials" +
                                   GetExtensionForMaterialList(settings.ModelExportFormat);

                    if (exportEachPass)
                    {
                        if (settings.ExportAllCharacterToSingleFolder && wldFile.WldType == WldType.Characters)
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
            }
            

            if (!exportEachPass)
            {
                var filePath = exportFolder + wldFile.ZoneShortname + "_materials" + GetExtensionForMaterialList(settings.ModelExportFormat);
                materialListWriter.WriteAssetToFile(filePath);
            }

            // Exporting meshes
            if (meshFragments != null)
            {
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
                        if (wldFile.WldType == WldType.Characters && settings.ExportAllCharacterToSingleFolder)
                        {
                            if (!((Mesh) fragment).MaterialList.HasBeenExported)
                            {
                                meshWriter.ClearExportData();
                                collisionMeshWriter.ClearExportData();
                                continue;
                            }
                        }

                        meshWriter.WriteAssetToFile(exportFolder + FragmentNameCleaner.CleanName(fragment) +
                                                    GetExtensionForMesh(settings.ModelExportFormat));
                        meshWriter.ClearExportData();

                        if (exportCollisionMesh)
                        {
                            collisionMeshWriter.WriteAssetToFile(exportFolder +
                                                                 FragmentNameCleaner.CleanName(fragment) +
                                                                 "_collision" +
                                                                 GetExtensionForMesh(settings.ModelExportFormat));
                            collisionMeshWriter.ClearExportData();
                        }
                    }
                }
            }
  
            
            foreach (var alternateMesh in alternateMeshFragments)
            {
                alternateMeshWriter.AddFragmentData(alternateMesh);

                // Determine if we need collision
                // if (exportEachPass)
                // {
                //     AlternateMesh mesh = fragment as AlternateMesh;
                //     exportCollisionMesh = mesh != null && mesh.ExportSeparateCollision;
                // }

                // if (exportCollisionMesh)
                // {
                //     collisionMeshWriter.AddFragmentData(fragment);
                // }

                if (exportEachPass)
                {
                    // TODO: Fix this mess
                    // if (wldFile.WldType == WldType.Characters && settings.ExportAllCharacterToSingleFolder)
                    // {
                    //     if (!alternateMesh.MaterialList.HasBeenExported)
                    //     {
                    //         meshWriter.ClearExportData();
                    //         collisionMeshWriter.ClearExportData();
                    //         continue;
                    //     }
                    // }
                    
                    alternateMeshWriter.WriteAssetToFile(exportFolder + FragmentNameCleaner.CleanName(alternateMesh) + GetExtensionForMesh(settings.ModelExportFormat));
                    alternateMeshWriter.ClearExportData();

                    if (exportCollisionMesh)
                    {
                        collisionMeshWriter.WriteAssetToFile(exportFolder + FragmentNameCleaner.CleanName(alternateMesh) + "_collision" + GetExtensionForMesh(settings.ModelExportFormat));
                        collisionMeshWriter.ClearExportData();
                    }
                }
            }

            if (!exportEachPass)
            {
                meshWriter.WriteAssetToFile(exportFolder + wldFile.ZoneShortname + GetExtensionForMesh(settings.ModelExportFormat));

                if (exportCollisionMesh)
                {
                    collisionMeshWriter.WriteAssetToFile(exportFolder + wldFile.ZoneShortname + "_collision" + GetExtensionForMesh(settings.ModelExportFormat));
                }
            }
        }

        private static string GetExportFolderForExportFormat(string rootFolder, ModelExportFormat format)
        {
            switch (format)
            {
                case ModelExportFormat.Intermediate:
                    return rootFolder + "/Meshes/";
                case ModelExportFormat.Obj:
                    return rootFolder + "/";
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
             
            throw new NotImplementedException();
        }

        private static string GetExtensionForMaterialList(ModelExportFormat format)
        {
            switch (format)
            {
                case ModelExportFormat.Intermediate:
                    return ".txt";
                case ModelExportFormat.Obj:
                    return ".mtl";
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static string GetExtensionForMesh(ModelExportFormat format)
        {
            switch (format)
            {
                case ModelExportFormat.Intermediate:
                    return ".txt";
                case ModelExportFormat.Obj:
                    return ".obj";
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}