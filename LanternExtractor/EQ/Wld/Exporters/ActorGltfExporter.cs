using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.EQ.Wld.Helpers;
using static LanternExtractor.EQ.Wld.Exporters.GltfWriter;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public static class ActorGltfExporter
    {
        public static void ExportActors(WldFile wldFile, Settings settings, ILogger logger)
        {
            // For a zone wld, we ignore actors and just export all meshes
            if (wldFile.WldType == WldType.Zone)
            {
                ExportZone((WldFileZone)wldFile, settings, logger);
                return;
            }

            foreach (var actor in wldFile.GetFragmentsOfType<Actor>())
            {
                switch (actor.ActorType)
                {
                    case ActorType.Static:
                        ExportStaticActor(actor, settings, wldFile, logger);
                        break;
                    case ActorType.Skeletal:
                        ExportSkeletalActor(actor, settings, wldFile, logger);
                        break;
                    default:
                        continue;
                }
            }
        }

        private static void ExportZone(WldFileZone wldFileZone, Settings settings, ILogger logger )
        {
            var zoneMeshes = wldFileZone.GetFragmentsOfType<Mesh>();
            var actors = new List<Actor>();
            var materialLists = wldFileZone.GetFragmentsOfType<MaterialList>();
            var objects = new List<ObjectInstance>();
            var shortName = wldFileZone.ShortName;
            var exportFormat = settings.ExportGltfInGlbFormat ? GltfExportFormat.Glb : GltfExportFormat.GlTF;

            if (settings.ExportZoneWithObjects)
            {
                var rootFolder = wldFileZone.RootFolder; 

                // Get object instances within this zone file to map up and instantiate later
                var zoneObjectsFileInArchive = wldFileZone.BaseS3DArchive.GetFile("objects" + LanternStrings.WldFormatExtension);
                if (zoneObjectsFileInArchive != null)
                {
                    var zoneObjectsWldFile = new WldFileZoneObjects(zoneObjectsFileInArchive, shortName,
                        WldType.ZoneObjects, logger, settings, wldFileZone.WldFileToInject);
                    zoneObjectsWldFile.Initialize(rootFolder, false);
                    objects.AddRange(zoneObjectsWldFile.GetFragmentsOfType<ObjectInstance>()
                        .Where(o => !o.ObjectName.Contains("door")));
                }

                // Find associated _obj archive e.g. qeytoqrg_obj.s3d, open it and add meshes and materials to our list
                var objPath = wldFileZone.BasePath.Replace(".s3d", "_obj.s3d");
                var objArchive = Path.GetFileNameWithoutExtension(objPath);
                var s3dObjArchive = new PfsArchive(objPath, logger);
                if (s3dObjArchive.Initialize())
                {
                    string wldFileName = objArchive + LanternStrings.WldFormatExtension;
                    var objWldFile = new WldFileZone(s3dObjArchive.GetFile(wldFileName), shortName, WldType.Objects, logger, settings);
                    objWldFile.Initialize(rootFolder, false);
                    ArchiveExtractor.WriteWldTextures(s3dObjArchive, objWldFile, rootFolder + shortName + "/Zone/Textures/", logger);
                    actors.AddRange(objWldFile.GetFragmentsOfType<Actor>());
                    materialLists.AddRange(objWldFile.GetFragmentsOfType<MaterialList>());
                }
            }

            if (!zoneMeshes.Any())
            {
                return;
            }

            var gltfWriter = new GltfWriter(settings.ExportGltfVertexColors, exportFormat, logger);
            var textureImageFolder = $"{wldFileZone.GetExportFolderForWldType()}Textures/";
            gltfWriter.GenerateGltfMaterials(materialLists, textureImageFolder);

            foreach (var mesh in zoneMeshes)
            {
                gltfWriter.AddFragmentData(
                    mesh: mesh, 
                    generationMode: ModelGenerationMode.Combine, 
                    meshNameOverride: shortName,
                    isZoneMesh: true);
            }
            gltfWriter.AddCombinedMeshToScene(true, shortName);

            foreach (var actor in actors)
            {
                if (actor.ActorType == ActorType.Static)
                {
                    var objMesh = actor.MeshReference?.Mesh;
                    if (objMesh == null) continue;

                    var instances = objects.FindAll(o => objMesh.Name.StartsWith(o.ObjectName, StringComparison.InvariantCultureIgnoreCase));
                    var instanceIndex = 0;
                    foreach (var instance in instances)
                    {
                        if (instance.Position.z < short.MinValue) continue;
                        // TODO: this could be more nuanced, I think this still exports trees below the zone floor

                        gltfWriter.AddFragmentData(
                            mesh: objMesh, 
                            generationMode: ModelGenerationMode.Separate,
                            objectInstance: instance, 
                            instanceIndex: instanceIndex++,
                            isZoneMesh: true);
                    }
                }
                else if (actor.ActorType == ActorType.Skeletal)
                {
                    var skeleton = actor.SkeletonReference?.SkeletonHierarchy;
                    if (skeleton == null) continue;

                    var instances = objects.FindAll(o => skeleton.Name.StartsWith(o.ObjectName, StringComparison.InvariantCultureIgnoreCase));
                    var instanceIndex = 0;
                    var combinedMeshName = FragmentNameCleaner.CleanName(skeleton);
                    var addedMeshOnce = false;

                    foreach (var instance in instances)
                    {
                        if (instance.Position.z < short.MinValue) continue;

                        if (!addedMeshOnce || 
                            (settings.ExportGltfVertexColors 
                                && instance.Colors?.Colors != null 
                                && instance.Colors.Colors.Any()))
                        {
                            for (int i = 0; i < skeleton.Skeleton.Count; i++)
                            {
                                var bone = skeleton.Skeleton[i];
                                var mesh = bone?.MeshReference?.Mesh;
                                if (mesh != null)
                                {
                                    var originalVertices = MeshExportHelper.ShiftMeshVertices(mesh, skeleton, false, "pos", 0, i);
                                    gltfWriter.AddFragmentData(
                                        mesh: mesh, 
                                        generationMode: ModelGenerationMode.Combine,
                                        meshNameOverride: combinedMeshName, 
                                        singularBoneIndex: i, 
                                        objectInstance: instance, 
                                        instanceIndex: instanceIndex,
                                        isZoneMesh: true);
                                    mesh.Vertices = originalVertices;
                                }
                            }
                        }

                        gltfWriter.AddCombinedMeshToScene(true, combinedMeshName, null, instance);
                        addedMeshOnce = true;
                        instanceIndex++;
                    }
                }
            }

            var exportFilePath = $"{wldFileZone.GetExportFolderForWldType()}{wldFileZone.ZoneShortname}.gltf";
            gltfWriter.WriteAssetToFile(exportFilePath, true);
        }

        private static void ExportStaticActor(Actor actor, Settings settings, WldFile wldFile, ILogger logger)
        {
            var mesh = actor?.MeshReference?.Mesh;

            if (mesh == null) return;

            var exportFormat = settings.ExportGltfInGlbFormat ? GltfExportFormat.Glb : GltfExportFormat.GlTF;
            var gltfWriter = new GltfWriter(settings.ExportGltfVertexColors, exportFormat, logger);
            
            var exportFolder = wldFile.GetExportFolderForWldType();

            // HACK - the helper method GetExportFolderForWldType() is looking at
            // this setting and returns the base zone folder if true
            if (settings.ExportCharactersToSingleFolder && wldFile.WldType == WldType.Characters)
            {
                exportFolder = $"{exportFolder}Characters/";
            }
            var textureImageFolder = $"{exportFolder}Textures/";
            gltfWriter.GenerateGltfMaterials(new List<MaterialList>() { mesh.MaterialList }, textureImageFolder);
            gltfWriter.AddFragmentData(mesh);
           
            var exportFilePath = $"{exportFolder}{FragmentNameCleaner.CleanName(mesh)}.gltf";
            gltfWriter.WriteAssetToFile(exportFilePath, true);
        }

        private static void ExportSkeletalActor(Actor actor, Settings settings, WldFile wldFile, ILogger logger)
        {
            var skeleton = actor?.SkeletonReference?.SkeletonHierarchy;

            if (skeleton == null) return;

            if (settings.ExportAdditionalAnimations && wldFile.ZoneShortname != "global")
            {
                GlobalReference.CharacterWld.AddAdditionalAnimationsToSkeleton(skeleton);
            }

            var exportFormat = settings.ExportGltfInGlbFormat ? GltfExportFormat.Glb : GltfExportFormat.GlTF;
            var gltfWriter = new GltfWriter(settings.ExportGltfVertexColors, exportFormat, logger);
            
            var materialLists = new HashSet<MaterialList>();
            var skeletonMeshMaterialList = skeleton.Meshes?.FirstOrDefault()?.MaterialList;
            if (skeletonMeshMaterialList != null)
            {
                materialLists.Add(skeletonMeshMaterialList);
            }

            foreach (var skeletonBones in skeleton.Skeleton)
            {
                var boneMaterialList = skeletonBones.MeshReference?.Mesh?.MaterialList;
                if (boneMaterialList != null)
                {
                    materialLists.Add(boneMaterialList);
                }
            }

            var exportFolder = wldFile.GetExportFolderForWldType();
            // HACK - the helper method GetExportFolderForWldType() is looking at
            // this setting and returns the base zone folder if true
            if (settings.ExportCharactersToSingleFolder && wldFile.WldType == WldType.Characters)
            {
                exportFolder = $"{exportFolder}Characters/";
            }
            var textureImageFolder = $"{exportFolder}Textures/";
            gltfWriter.GenerateGltfMaterials(materialLists, textureImageFolder);

            for (int i = 0; i < skeleton.Skeleton.Count; i++)
            {
                var bone = skeleton.Skeleton[i];
                var mesh = bone?.MeshReference?.Mesh;
                if (mesh != null)
                {
                    MeshExportHelper.ShiftMeshVertices(mesh, skeleton,
                        wldFile.WldType == WldType.Characters, "pos", 0, i);

                    gltfWriter.AddFragmentData(mesh, skeleton, null, i);
                }
            }
            if (skeleton.Meshes != null)
            {
                foreach (var mesh in skeleton.Meshes)
                {
                    MeshExportHelper.ShiftMeshVertices(mesh, skeleton,
                        wldFile.WldType == WldType.Characters, "pos", 0);

                    gltfWriter.AddFragmentData(mesh, skeleton);
                }

                for (var i = 0; i < skeleton.SecondaryMeshes.Count; i++)
                {
                    var secondaryMesh = skeleton.SecondaryMeshes[i];
                    var secondaryGltfWriter = new GltfWriter(settings.ExportGltfVertexColors, exportFormat, logger);
                    secondaryGltfWriter.CopyMaterialList(gltfWriter);
                    secondaryGltfWriter.AddFragmentData(skeleton.Meshes[0], skeleton);

                    MeshExportHelper.ShiftMeshVertices(secondaryMesh, skeleton,
                        wldFile.WldType == WldType.Characters, "pos", 0);
                    secondaryGltfWriter.AddFragmentData(secondaryMesh, skeleton);
                    secondaryGltfWriter.ApplyAnimationToSkeleton(skeleton, "pos", wldFile.WldType == WldType.Characters, true);

                    if (settings.ExportAllAnimationFrames)
                    {
                        secondaryGltfWriter.AddFragmentData(secondaryMesh, skeleton);
                        foreach (var animationKey in skeleton.Animations.Keys)
                        {
                            secondaryGltfWriter.ApplyAnimationToSkeleton(skeleton, animationKey, wldFile.WldType == WldType.Characters, false);
                        }
                    }
                    
                    var secondaryExportPath = $"{exportFolder}{FragmentNameCleaner.CleanName(skeleton)}_{i:00}.gltf";
                    secondaryGltfWriter.WriteAssetToFile(secondaryExportPath, true, skeleton.ModelBase);
                }
            }

            gltfWriter.ApplyAnimationToSkeleton(skeleton, "pos", wldFile.WldType == WldType.Characters, true);

            if (settings.ExportAllAnimationFrames)
            {
                foreach (var animationKey in skeleton.Animations.Keys)
                {
                    gltfWriter.ApplyAnimationToSkeleton(skeleton, animationKey, wldFile.WldType == WldType.Characters, false);
                }
            }
           
            var exportFilePath = $"{exportFolder}{FragmentNameCleaner.CleanName(skeleton)}.gltf";
            gltfWriter.WriteAssetToFile(exportFilePath, true, skeleton.ModelBase);

            // TODO: bother with skin variants? If GLTF can just copy the .gltf and change the
            // corresponding image URIs. If GLB then would have to repackage every variant.
            // KHR_materials_variants extension is made for this, but no support for it in SharpGLTF
        }
    }
}
