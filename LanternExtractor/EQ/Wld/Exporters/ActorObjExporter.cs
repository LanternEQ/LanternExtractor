using System.Collections.Generic;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public static class ActorObjExporter
    {
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

            var meshWriter = new MeshObjWriter(ObjExportType.Textured, settings.ExportHiddenGeometry,
                settings.ExportZoneMeshGroups, wldFile.ZoneShortname);
            var materialListWriter = new MeshObjMtlWriter(settings, wldFile.ZoneShortname);
            
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
                    ShiftVertices(mesh, skeleton);
                    meshWriter.AddFragmentData(mesh);
                }
            }

            meshWriter.WriteAssetToFile(GetMeshPath(wldFile, FragmentNameCleaner.CleanName(skeleton)));

            var materialList = skeleton.Meshes?[0].MaterialList;

            if (materialList != null)
            {
                materialListWriter.AddFragmentData(materialList);
                materialListWriter.WriteAssetToFile(GetMaterialListPath(wldFile, FragmentNameCleaner.CleanName(materialList)));
            }
        }

        private static string GetMeshPath(WldFile wldFile, string meshName)
        {
            return wldFile.GetExportFolderForWldType() +
                   meshName + ".obj";
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

        private static void ShiftVertices(Mesh mesh, SkeletonHierarchy skeleton)
        {
            foreach (var something in mesh.MobPieces)
            {
                var boneIndex = something.Key;
                var bone = skeleton.Tree[boneIndex];
                var matrix = bone.PoseMatrix;

                for (int i = 0; i < something.Value.Count; ++i)
                {
                    var vertex = mesh.Vertices[i + something.Value.Start];
                    var newVertex = matrix * new vec4(vertex, 1f);
                    mesh.Vertices[i + something.Value.Start] = newVertex.xyz;
                }
            }
        }
    }
}