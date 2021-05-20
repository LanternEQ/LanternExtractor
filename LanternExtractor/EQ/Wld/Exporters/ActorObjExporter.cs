using System.Collections.Generic;
using System.Linq;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public static class ActorObjExporter
    {
        public static Dictionary<Mesh, List<vec3>> _backupVertices = new Dictionary<Mesh, List<vec3>>();
        
        public static void ExportActors(WldFile wldFile, Settings settings, ILogger logger)
        {
            // For a zone wld, we ignore actors and just export all meshes
            if (wldFile.WldType == WldType.Zone)
            {
                ExportZone(wldFile, settings, logger);
                return;
            }

            List<Actor> actors = wldFile.GetFragmentsOfType<Actor>();

            foreach (var actor in actors)
            {
                switch (actor.ActorType)
                {
                    case ActorType.Static:
                        ExportStaticActor(actor, settings, wldFile);
                        break;
                    case ActorType.Skeletal:
                        ExportSkeletalActor(actor, settings, wldFile);
                        break;
                    default:
                        continue;
                }
            }
        }

        /// <summary>
        /// Exports all of the meshes of a zone in a single obj
        /// </summary>
        /// <param name="wldFile"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        private static void ExportZone(WldFile wldFile, Settings settings, ILogger logger)
        {
            var meshes = wldFile.GetFragmentsOfType<Mesh>();

            if (meshes.Count == 0)
            {
                return;
            }
            
            var materialList = meshes[0].MaterialList;

            var meshWriter = new MeshObjWriter(ObjExportType.Textured, settings.ExportHiddenGeometry,
                settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
            var collisionMeshWriter = new MeshObjWriter(ObjExportType.Collision, settings.ExportHiddenGeometry,
                settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
            var materialListWriter = new MeshObjMtlWriter(settings, wldFile.ZoneShortname);

            foreach (var mesh in meshes)
            {
                meshWriter.AddFragmentData(mesh);
                collisionMeshWriter.AddFragmentData(mesh);
            }

            meshWriter.WriteAssetToFile(GetMeshPath(wldFile, wldFile.ZoneShortname));
            collisionMeshWriter.WriteAssetToFile(GetCollisionMeshPath(wldFile, wldFile.ZoneShortname));

            materialListWriter.AddFragmentData(materialList);
            materialListWriter.WriteAssetToFile(GetMaterialListPath(wldFile, FragmentNameCleaner.CleanName(materialList)));
        }

        private static void ExportStaticActor(Actor actor, Settings settings, WldFile wldFile)
        {
            var mesh = actor?.MeshReference?.Mesh;

            if (mesh == null)
            {
                return;
            }

            var meshWriter = new MeshObjWriter(ObjExportType.Textured, settings.ExportHiddenGeometry,
                settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
            var collisionMeshWriter = new MeshObjWriter(ObjExportType.Collision, settings.ExportHiddenGeometry,
                settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
            var materialListWriter = new MeshObjMtlWriter(settings, wldFile.ZoneShortname);

            meshWriter.AddFragmentData(mesh);
            meshWriter.WriteAssetToFile(GetMeshPath(wldFile, FragmentNameCleaner.CleanName(mesh)));

            if (mesh.ExportSeparateCollision)
            {
                collisionMeshWriter.AddFragmentData(mesh);
                collisionMeshWriter.WriteAssetToFile(GetCollisionMeshPath(wldFile, FragmentNameCleaner.CleanName(mesh)));
            }

            var materialList = mesh.MaterialList;
            materialListWriter.AddFragmentData(materialList);
            materialListWriter.WriteAssetToFile(GetMaterialListPath(wldFile, FragmentNameCleaner.CleanName(materialList)));
        }

        private static void ExportSkeletalActor(Actor actor, Settings settings, WldFile wldFile)
        {
            var skeleton = actor?.SkeletonReference?.SkeletonHierarchy;

            if (skeleton == null)
            {
                return;
            }

            if (settings.ExportAllAnimationFrames)
            {
                foreach (var animation in skeleton.Animations)
                {
                    for (int i = 0; i < animation.Value.FrameCount; ++i)
                    {
                        WriteAnimationFrame(skeleton, animation.Key, i, settings, wldFile);
                    }
                }
            }
            else
            {
                WriteAnimationFrame(skeleton, "pos", 0, settings, wldFile);
            }

            List<MaterialList> materialLists = new List<MaterialList>();
            
            var materialListWriter = new MeshObjMtlWriter(settings, wldFile.ZoneShortname);

            var materialList = skeleton.Meshes?.FirstOrDefault()?.MaterialList;

            if (materialList != null)
            {
                materialLists.Add(materialList);
            }
            
            foreach (var skeletonBones in skeleton.Skeleton)
            {
                var boneMaterialList = skeletonBones.MeshReference?.Mesh?.MaterialList;

                if (boneMaterialList != null)
                {
                    if (!materialLists.Contains(boneMaterialList))
                    {
                        materialLists.Add(boneMaterialList);
                    }
                }
            }

            foreach(var ml in materialLists)
            {
                materialListWriter.AddFragmentData(ml);

                string savePath;

                if (wldFile.WldType == WldType.Characters)
                {
                    savePath = GetMaterialListPath(wldFile, FragmentNameCleaner.CleanName(ml), 0);
                }
                else
                {
                    savePath = GetMaterialListPath(wldFile, FragmentNameCleaner.CleanName(ml));
                }
                
                materialListWriter.WriteAssetToFile(savePath);

                for (int i = 0; i < ml.VariantCount; ++i)
                {
                    materialListWriter.ClearExportData();
                    materialListWriter.SetSkinId(i + 1);
                    materialListWriter.AddFragmentData(ml);
                    materialListWriter.WriteAssetToFile(GetMaterialListPath(wldFile, FragmentNameCleaner.CleanName(ml), i + 1));
                }
                
                materialListWriter.ClearExportData();
            }
        }

        private static void WriteAnimationFrame(SkeletonHierarchy skeleton, string animation, int frameIndex, Settings settings, WldFile wldFile)
        {
            var meshWriter = new MeshObjWriter(ObjExportType.Textured, settings.ExportHiddenGeometry,
                settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
            
            meshWriter.SetIsCharacterModel(wldFile.WldType == WldType.Characters);
            
            foreach (var bone in skeleton.Skeleton)
            {
                var mesh = bone?.MeshReference?.Mesh;

                if (mesh != null)
                {
                    meshWriter.AddFragmentData(mesh);
                }
            }
            
            if (skeleton.Meshes != null)
            {
                foreach (var mesh in skeleton.Meshes)
                {
                    ShiftVertices(mesh, skeleton, animation, frameIndex);
                    meshWriter.AddFragmentData(mesh);
                }

                for (var i = 0; i < skeleton.SecondaryMeshes.Count; i++)
                {
                    var m2 = skeleton.SecondaryMeshes[i];
                    var meshWriter2 = new MeshObjWriter(ObjExportType.Textured, settings.ExportHiddenGeometry,
                        settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
                    meshWriter2.SetIsCharacterModel(true);
                    meshWriter2.AddFragmentData(skeleton.Meshes[0]);
                    ShiftVertices(m2, skeleton, animation, frameIndex);
                    meshWriter2.AddFragmentData(m2);
                    meshWriter2.WriteAssetToFile(GetMeshPath(wldFile, FragmentNameCleaner.CleanName(skeleton), i + 1));
                }
            }

            string fileName = string.Empty;

            if (settings.ExportAllAnimationFrames)
            {
                fileName = FragmentNameCleaner.CleanName(skeleton) + "_" + animation + "_" + frameIndex;
            }
            else
            {
                fileName = FragmentNameCleaner.CleanName(skeleton);
            }
            
            meshWriter.WriteAssetToFile(GetMeshPath(wldFile, fileName));
            RestoreVertices();
        }

        private static void RestoreVertices()
        {
            foreach (var shiftedMesh in _backupVertices)
            {
                shiftedMesh.Key.Vertices = shiftedMesh.Value;
            }
        }

        private static void ShiftVertices(Mesh mesh, SkeletonHierarchy skeleton, string animName, int frame)
        {
            if (!skeleton.Animations.ContainsKey(animName))
            {
                return;
            }

            if (mesh.Vertices.Count == 0)
            {
                return;
            }
            
            _backupVertices[mesh] = new List<vec3>();

            var animation = skeleton.Animations[animName];
            
            foreach (var mobVertexPiece in mesh.MobPieces)
            {
                var boneIndex = mobVertexPiece.Key;
                var bone = skeleton.Skeleton[boneIndex].CleanedName;

                if (!animation.TracksCleanedStripped.ContainsKey(bone))
                {
                    continue;
                }
                
                mat4 modelMatrix = skeleton.GetBoneMatrix(boneIndex, animName, frame);
              
                for (int i = 0; i < mobVertexPiece.Value.Count; ++i)
                {
                    int shiftedIndex = i + mobVertexPiece.Value.Start;

                    if (shiftedIndex >= mesh.Vertices.Count)
                    {
                        continue;
                    }
                    
                    var vertex = mesh.Vertices[shiftedIndex];
                    _backupVertices[mesh].Add(vertex);
                    var newVertex = modelMatrix * new vec4(vertex, 1f);
                    mesh.Vertices[i + mobVertexPiece.Value.Start] = newVertex.xyz;
                }
            }
        }

        private static string GetMeshPath(WldFile wldFile, string meshName)
        {
            return wldFile.GetExportFolderForWldType() +
                   meshName + ".obj";
        }

        private static string GetMeshPath(WldFile wldFile, string meshName, int variant)
        {
            return wldFile.GetExportFolderForWldType() +
                   meshName + "_" + variant + ".obj";
        }

        private static string GetCollisionMeshPath(WldFile wldFile, string meshName)
        {
            return wldFile.GetExportFolderForWldType() +
                   meshName + "_collision.obj";
        }

        private static string GetMaterialListPath(WldFile wldFile, string materialListName)
        {
            return wldFile.GetExportFolderForWldType() + "/" +
                   materialListName + ".mtl";
        }

        private static string GetMaterialListPath(WldFile wldFile, string materialListName, int skinIndex)
        {
            return wldFile.GetExportFolderForWldType() + "/" +
                   materialListName + "_" + skinIndex + ".mtl";
        }
    }
}