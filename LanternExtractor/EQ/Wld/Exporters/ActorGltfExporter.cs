using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GlmSharp;

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
            return;

            foreach (var actor in wldFile.GetFragmentsOfType<Actor>())
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

        private static void ExportZone(WldFileZone wldFileZone, Settings settings, ILogger logger )
        {
            var meshes = wldFileZone.GetFragmentsOfType<Mesh>();
            var materialLists = wldFileZone.GetFragmentsOfType<MaterialList>();
            var objects = new List<ObjectInstance>();

            if (settings.ExportZoneWithObjects)
            {
                var rootFolder = wldFileZone.RootFolder;
                var shortName = wldFileZone.ShortName;

                // Get object instances within this zone file to map up and instantiate later
                var zoneObjectsFileInArchive = wldFileZone.BaseS3DArchive.GetFile("objects" + LanternStrings.WldFormatExtension);
                if (zoneObjectsFileInArchive != null)
                {
                    var zoneObjectsWldFile = new WldFileZoneObjects(zoneObjectsFileInArchive, shortName,
                        WldType.ZoneObjects, logger, settings, wldFileZone.WldFileToInject);
                    zoneObjectsWldFile.Initialize(rootFolder, false);
                    objects.AddRange(zoneObjectsWldFile.GetFragmentsOfType<ObjectInstance>());
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
                    meshes.AddRange(objWldFile.GetFragmentsOfType<Mesh>());
                    materialLists.AddRange(objWldFile.GetFragmentsOfType<MaterialList>());
                }
            }

            if (!meshes.Any())
            {
                return;
            }

            var textureImageFolder = $"{wldFileZone.GetExportFolderForWldType()}Textures/";
            var gltfMaterials = GenerateGltfMaterials(materialLists, textureImageFolder, settings);
            var scene = new SharpGLTF.Scenes.SceneBuilder();

            AddMeshesToScene(scene, meshes, gltfMaterials, settings, wldFileZone.ZoneShortname);

            var model = scene.ToGltf2();
            var exportFilePath = $"{wldFileZone.GetExportFolderForWldType()}{wldFileZone.ZoneShortname}.gltf";
            model.SaveGLTF(exportFilePath);
        }

        private static void ExportStaticActor(Actor actor, Settings settings, WldFile wldFile )
        {

        }

        private static void ExportSkeletalActor(Actor actor, Settings settings, WldFile wldFile)
        {

        }

        private static MaterialBuilder GetBlankMaterial()
        {
            return new MaterialBuilder(MaterialBlankName)
                .WithDoubleSide(false)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 1, 0));
        }

        private static MaterialBuilder GetInvisibleMaterial()
        {
            return new MaterialBuilder(MaterialInvisName)
                .WithDoubleSide(false)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 1, 1))
                .WithAlpha(AlphaMode.MASK);
        }

        private static IDictionary<string, MaterialBuilder> GenerateGltfMaterials(IList<MaterialList> materialLists, string textureImageFolder, Settings settings)
        {
            var gltfMaterials = new Dictionary<string, MaterialBuilder>();
            gltfMaterials.Add(MaterialBlankName, GetBlankMaterial());
            gltfMaterials.Add(MaterialInvisName, GetInvisibleMaterial());

            foreach (var materialList in materialLists)
            {
                if (materialList == null)
                {
                    continue;
                }

                foreach (var eqMaterial in materialList.Materials)
                {
                    var materialName = GetMaterialName(eqMaterial);

                    if (gltfMaterials.ContainsKey(materialName)) continue;

                    var imageFileNameWithoutExtension = eqMaterial.GetFirstBitmapNameWithoutExtension();

                    if (string.IsNullOrEmpty(imageFileNameWithoutExtension)) continue;       
                    if (eqMaterial.ShaderType == ShaderType.Invisible) continue;

                    var gltfMaterial = new MaterialBuilder(materialName)
                        .WithDoubleSide(false)
                        .WithMetallicRoughnessShader()
                        .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, MaterialRoughness)
                        .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, 0f)
                        .WithChannelImage(KnownChannel.BaseColor, $"{textureImageFolder}{eqMaterial.GetFirstBitmapExportFilename()}");

                    switch (eqMaterial.ShaderType)
                    {
                        case ShaderType.Transparent25:
                            gltfMaterial.WithAlpha(AlphaMode.MASK, 0.25f);
                            break;
                        case ShaderType.Transparent50:
                            gltfMaterial.WithAlpha(AlphaMode.MASK, 0.5f);
                            break;
                        case ShaderType.Transparent75:
                            gltfMaterial.WithAlpha(AlphaMode.MASK, 0.75f);
                            break;
                        case ShaderType.TransparentAdditive:
                        case ShaderType.TransparentAdditiveUnlit:
                        case ShaderType.TransparentMasked:
                        case ShaderType.TransparentSkydome:
                        case ShaderType.TransparentAdditiveUnlitSkydome:
                            gltfMaterial.WithAlpha(AlphaMode.BLEND);
                            break;
                        default:
                            gltfMaterial.WithAlpha(AlphaMode.OPAQUE);
                            break;
                    }

                    if (eqMaterial.ShaderType == ShaderType.TransparentAdditiveUnlit ||
                        eqMaterial.ShaderType == ShaderType.DiffuseSkydome ||
                        eqMaterial.ShaderType == ShaderType.TransparentAdditiveUnlitSkydome)
                    {
                        gltfMaterial.WithUnlitShader();
                    }

                    gltfMaterials.Add(materialName, gltfMaterial);
                }
            }

            return gltfMaterials;
        }
        private static string GetMaterialName(Material eqMaterial)
        {
            return $"{MaterialList.GetMaterialPrefix(eqMaterial.ShaderType)}_{eqMaterial.GetFirstBitmapNameWithoutExtension()}";
        }

        private static void AddMeshesToScene(SharpGLTF.Scenes.SceneBuilder scene, 
            IEnumerable<Mesh> meshes, IDictionary<string, MaterialBuilder> gltfMaterials, 
            Settings settings, string singleMeshName = null)
        {
            var exportVertexColors = false;
            // Result needs to be mirrored along the X axis
            var transformMatrix = Matrix4x4.CreateReflection(new Plane(1, 0, 0, 0));
            IMeshBuilder<MaterialBuilder> gltfMesh = null;
            foreach (var mesh in meshes)
            {
                var polygonIndex = 0;
                var meshName = singleMeshName ?? mesh.Name ?? $"Mesh_{scene.Instances.Count}";
                if (gltfMesh == null || singleMeshName == null)
                {
                    if (exportVertexColors)
                    {
                        gltfMesh = new MeshBuilder<VertexPositionNormal, VertexColor1Texture1, VertexEmpty>(meshName);
                    }
                    else
                    {
                        gltfMesh = new MeshBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>(meshName);
                    }
                }
                foreach (var materialGroup in mesh.MaterialGroups)
                {
                    var materialName = GetMaterialName(mesh.MaterialList.Materials[materialGroup.MaterialIndex]);
                    if (!gltfMaterials.TryGetValue(materialName, out var gltfMaterial))
                    {
                        gltfMaterial = gltfMaterials[MaterialBlankName];
                    }

                    var primitive = gltfMesh.UsePrimitive(gltfMaterial);
                    for (var i = 0; i < materialGroup.PolygonCount; ++i)
                    {
                        var triangle = mesh.Indices[polygonIndex++];
                        (int v0, int v1, int v2) vertexIndices = (triangle.Vertex1, triangle.Vertex2, triangle.Vertex3);
                        (Vector3 v0, Vector3 v1, Vector3 v2) vertexPositions = (
                            (mesh.Vertices[vertexIndices.v0] + mesh.Center).ToVector3(true),
                            (mesh.Vertices[vertexIndices.v1] + mesh.Center).ToVector3(true),
                            (mesh.Vertices[vertexIndices.v2] + mesh.Center).ToVector3(true));
                        (Vector3 v0, Vector3 v1, Vector3 v2) vertexNormals = (
                            -mesh.Normals[vertexIndices.v0].ToVector3(),
                            -mesh.Normals[vertexIndices.v1].ToVector3(),
                            -mesh.Normals[vertexIndices.v2].ToVector3());
                        (Vector2 v0, Vector2 v1, Vector2 v2) vertexUvs = (
                            mesh.TextureUvCoordinates[vertexIndices.v0].ToVector2(),
                            mesh.TextureUvCoordinates[vertexIndices.v1].ToVector2(),
                            mesh.TextureUvCoordinates[vertexIndices.v2].ToVector2());
                        if (exportVertexColors)
                        {
                            (Vector4 v0, Vector4 v1, Vector4 v2) vertexColors = (
                                mesh.Colors[vertexIndices.v0].ToVector4(),
                                mesh.Colors[vertexIndices.v1].ToVector4(),
                                mesh.Colors[vertexIndices.v2].ToVector4());
                            ((PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexColor1Texture1, VertexEmpty>)primitive)
                                .AddTriangle(
                                    ((vertexPositions.v0, vertexNormals.v0), (vertexColors.v0, vertexUvs.v0)),
                                    ((vertexPositions.v1, vertexNormals.v1), (vertexColors.v1, vertexUvs.v1)),
                                    ((vertexPositions.v2, vertexNormals.v2), (vertexColors.v2, vertexUvs.v2)));
                        }
                        else
                        {
                            ((PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexTexture1, VertexEmpty>)primitive)
                                .AddTriangle(
                                    ((vertexPositions.v0, vertexNormals.v0), (vertexUvs.v0)),
                                    ((vertexPositions.v1, vertexNormals.v1), (vertexUvs.v1)),
                                    ((vertexPositions.v2, vertexNormals.v2), (vertexUvs.v2)));
                        }

                    }
                    if (singleMeshName == null)
                    {                  
                        scene.AddRigidMesh(gltfMesh, transformMatrix);
                    }
                }
            }
            if (singleMeshName != null)
            {
                scene.AddRigidMesh(gltfMesh, transformMatrix);
            }
        }

        private static Vector2 ToVector2(this vec2 v2)
        {
            return new Vector2(v2.x, v2.y);
        }

        private static Vector3 ToVector3(this vec3 v3, bool swapYandZ = false)
        {
            if (swapYandZ)
            {
                return new Vector3(v3.x, v3.z, v3.y);
            }
            else
            {
                return new Vector3(v3.x, v3.y, v3.z);
            }
        }

        private static Vector4 ToVector4(this Color color)
        {
            return new Vector4(color.B, color.G, color.R, color.A);
        }
        private static float MaterialRoughness = 0.85f;
        private static string MaterialInvisName = "Invis";
        private static string MaterialBlankName = "Blank";
    }
}
