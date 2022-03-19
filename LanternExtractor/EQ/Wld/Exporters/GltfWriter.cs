using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class GltfWriter : TextAssetWriter
    {
        public enum GltfExportFormat
        {
            /// <summary>
            /// Separate .gltf json file, .bin binary, and images externally referenced
            /// </summary>
            GlTF = 0,
            /// <summary>
            /// One binary file with json metadata and images packaged within
            /// </summary>
            Glb = 1
        }

        public enum ModelGenerationMode
        {
            /// <summary>
            /// Combine all meshes
            /// </summary>
            Combine = 0,
            /// <summary>
            /// Every mesh remains separated
            /// </summary>
            Separate = 1
        }

        public IDictionary<string, MaterialBuilder> Materials { get; private set; }

        private readonly bool _exportVertexColors;
        private readonly GltfExportFormat _exportFormat = GltfExportFormat.GlTF;

        private static readonly float MaterialRoughness = 0.9f;
        private static readonly string MaterialInvisName = "Invis";
        private static readonly string MaterialBlankName = "Blank";
        private static readonly Matrix4x4 CorrectedWorldMatrix = Matrix4x4.CreateReflection(new Plane(1, 0, 0, 0))
            * Matrix4x4.CreateScale(0.1f);

        private SharpGLTF.Scenes.SceneBuilder _scene;
        private IMeshBuilder<MaterialBuilder> _combinedMeshBuilder;
        private ISet<string> _meshMaterialsToSkip;
        private IDictionary<string, IMeshBuilder<MaterialBuilder>> _sharedMeshes;
        private IDictionary<string, List<NodeBuilder>> _skeletons;

        public GltfWriter(bool exportVertexColors, GltfExportFormat exportFormat)
        {
            _exportVertexColors = exportVertexColors;
            _exportFormat = exportFormat;

            Materials = new Dictionary<string, MaterialBuilder>();
            _meshMaterialsToSkip = new HashSet<string>();
            _skeletons = new Dictionary<string, List<NodeBuilder>>();
            _sharedMeshes = new Dictionary<string, IMeshBuilder<MaterialBuilder>>();
            _scene = new SceneBuilder();
        }

        public override void AddFragmentData(WldFragment fragment)
        {
            AddFragmentData((Mesh)fragment, ModelGenerationMode.Separate);
        }

        public void AddFragmentData(Mesh mesh, SkeletonHierarchy skeleton,
            string meshNameOverride = null, int singularBoneIndex = -1)
        {
            if (!_skeletons.ContainsKey(skeleton.ModelBase))
            {
                AddNewSkeleton(skeleton);
            }

            AddFragmentData(mesh, ModelGenerationMode.Combine, true, meshNameOverride, singularBoneIndex);
        }

        public void CopyMaterialList(GltfWriter gltfWriter)
        {
            Materials = gltfWriter.Materials;
        }

        public void AddFragmentData(Mesh mesh, ModelGenerationMode generationMode, bool isSkinned = false, string meshNameOverride = null, 
            int singularBoneIndex = -1, ObjectInstance objectInstance = null, int instanceIndex = 0)
        {
            var meshName = meshNameOverride ?? FragmentNameCleaner.CleanName(mesh);
            var transformMatrix = objectInstance == null ? Matrix4x4.Identity : CreateTransformMatrixForObjectInstance(objectInstance);
            var canExportVertexColors = _exportVertexColors &&
                ((objectInstance?.Colors?.Colors != null && objectInstance.Colors.Colors.Any()) 
                || (mesh?.Colors != null && mesh.Colors.Any()));
            if (!canExportVertexColors && objectInstance != null && _sharedMeshes.TryGetValue(meshName, out var existingMesh))
            {
                if (generationMode == ModelGenerationMode.Separate)
                {
                    _scene.AddRigidMesh(existingMesh, transformMatrix * CorrectedWorldMatrix);
                }
                return;
            }
            IMeshBuilder<MaterialBuilder> gltfMesh;
            
            if (canExportVertexColors && objectInstance != null)
            {
                meshName += $".{instanceIndex:00}";
            }
            if (generationMode == ModelGenerationMode.Combine)
            {
                if (_combinedMeshBuilder == null)
                {
                    _combinedMeshBuilder = InstantiateMeshBuilder(meshName, isSkinned, canExportVertexColors);
                }
                gltfMesh = _combinedMeshBuilder;
            }
            else
            {             
                gltfMesh = InstantiateMeshBuilder(meshName, isSkinned, canExportVertexColors);
            }

            var polygonIndex = 0;
            foreach (var materialGroup in mesh.MaterialGroups)
            {
                var materialName = GetMaterialName(mesh.MaterialList.Materials[materialGroup.MaterialIndex]);
                if (_meshMaterialsToSkip.Contains(materialName))
                {
                    polygonIndex += materialGroup.PolygonCount;
                    continue;
                }

                if (!Materials.TryGetValue(materialName, out var gltfMaterial))
                {
                    gltfMaterial = Materials[MaterialBlankName];
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
                        mesh.TextureUvCoordinates[vertexIndices.v0].ToVector2(true),
                        mesh.TextureUvCoordinates[vertexIndices.v1].ToVector2(true),
                        mesh.TextureUvCoordinates[vertexIndices.v2].ToVector2(true));
                    (int v0, int v1, int v2) boneIndexes = (singularBoneIndex, singularBoneIndex, singularBoneIndex);
                    if (isSkinned && singularBoneIndex == -1)
                    {
                        boneIndexes = (
                            GetBoneIndexForVertex(mesh, vertexIndices.v0),
                            GetBoneIndexForVertex(mesh, vertexIndices.v1),
                            GetBoneIndexForVertex(mesh, vertexIndices.v2));
                    }
                    if (canExportVertexColors)
                    {
                        var vertexColors = GetVertexColorVectors(mesh, vertexIndices, objectInstance);
                        if (isSkinned)
                        {
                            ((PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexColor1Texture1, VertexJoints4>)primitive)
                                .AddTriangle(
                                    ((vertexPositions.v0, vertexNormals.v0), (vertexColors.v0, vertexUvs.v0), new VertexJoints4(boneIndexes.v0)),
                                    ((vertexPositions.v1, vertexNormals.v1), (vertexColors.v1, vertexUvs.v1), new VertexJoints4(boneIndexes.v1)),
                                    ((vertexPositions.v2, vertexNormals.v2), (vertexColors.v2, vertexUvs.v2), new VertexJoints4(boneIndexes.v2)));
                        }
                        else
                        {
                            ((PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexColor1Texture1, VertexEmpty>)primitive)
                                .AddTriangle(
                                    ((vertexPositions.v0, vertexNormals.v0), (vertexColors.v0, vertexUvs.v0)),
                                    ((vertexPositions.v1, vertexNormals.v1), (vertexColors.v1, vertexUvs.v1)),
                                    ((vertexPositions.v2, vertexNormals.v2), (vertexColors.v2, vertexUvs.v2)));
                        }
                    }
                    else // !canExportVertexColors
                    {
                        if (isSkinned)
                        {
                            ((PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexTexture1, VertexJoints4>)primitive)
                                .AddTriangle(
                                    ((vertexPositions.v0, vertexNormals.v0), vertexUvs.v0, new VertexJoints4(boneIndexes.v0)),
                                    ((vertexPositions.v1, vertexNormals.v1), vertexUvs.v1, new VertexJoints4(boneIndexes.v1)),
                                    ((vertexPositions.v2, vertexNormals.v2), vertexUvs.v2, new VertexJoints4(boneIndexes.v2)));
                        }
                        else
                        {
                            ((PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexTexture1, VertexEmpty>)primitive)
                                .AddTriangle(
                                    ((vertexPositions.v0, vertexNormals.v0), vertexUvs.v0),
                                    ((vertexPositions.v1, vertexNormals.v1), vertexUvs.v1),
                                    ((vertexPositions.v2, vertexNormals.v2), vertexUvs.v2));
                        }
                    }
                }
            }
            if (generationMode == ModelGenerationMode.Separate)
            {
                _scene.AddRigidMesh(gltfMesh, transformMatrix * CorrectedWorldMatrix);
                _sharedMeshes.Add(meshName, gltfMesh);
            }
        }

        public void GenerateGltfMaterials(IEnumerable<MaterialList> materialLists, string textureImageFolder)
        {
            if (!Materials.Any())
            {
                Materials.Add(MaterialBlankName, GetBlankMaterial());
                Materials.Add(MaterialInvisName, GetInvisibleMaterial());
            }

            foreach (var materialList in materialLists)
            {
                if (materialList == null)
                {
                    continue;
                }

                foreach (var eqMaterial in materialList.Materials)
                {
                    var materialName = GetMaterialName(eqMaterial);

                    if (Materials.ContainsKey(materialName)) continue;

                    var imageFileNameWithoutExtension = eqMaterial.GetFirstBitmapNameWithoutExtension();

                    if (string.IsNullOrEmpty(imageFileNameWithoutExtension)) continue;

                    if (eqMaterial.ShaderType == ShaderType.Boundary
                        || eqMaterial.ShaderType == ShaderType.Invisible)
                    {
                        _meshMaterialsToSkip.Add(materialName);
                        continue;
                    }

                    var imagePath = $"{textureImageFolder}{eqMaterial.GetFirstBitmapExportFilename()}";
                    var imageName = Path.GetFileNameWithoutExtension(imagePath);
                    var gltfMaterial = new MaterialBuilder(materialName)
                        .WithDoubleSide(false)
                        .WithMetallicRoughnessShader()
                        .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, MaterialRoughness)
                        .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, 0f);
                    // If we use the method below, the image name is not retained
                    //    .WithChannelImage(KnownChannel.BaseColor, $"{textureImageFolder}{eqMaterial.GetFirstBitmapExportFilename()}");
                    gltfMaterial.UseChannel(KnownChannel.BaseColor)
                        .UseTexture()
                        .WithPrimaryImage(ImageBuilder.From(new MemoryImage(imagePath), imageName));

                    switch (eqMaterial.ShaderType)
                    {
                        case ShaderType.Transparent25:
                            gltfMaterial.WithAlpha(AlphaMode.MASK, 0.25f);
                            break;
                        case ShaderType.Transparent50:
                        case ShaderType.TransparentMasked:
                            gltfMaterial.WithAlpha(AlphaMode.MASK, 0.5f);
                            break;
                        case ShaderType.Transparent75:
                            gltfMaterial.WithAlpha(AlphaMode.MASK, 0.75f);
                            break;
                        case ShaderType.TransparentAdditive:
                        case ShaderType.TransparentAdditiveUnlit:
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

                    Materials.Add(materialName, gltfMaterial);
                }
            }
        }

        public void ApplyAnimationToSkeleton(SkeletonHierarchy skeleton, string animationKey, int frame = -1)
        {
            if (!_skeletons.TryGetValue(skeleton.ModelBase, out var skeletonNodes))
            {
                skeletonNodes = AddNewSkeleton(skeleton);
            }
            var animation = skeleton.Animations[animationKey];
            var poseArray = animation.TracksCleanedStripped;
            if (frame > -1)
            {
                for (var i = 0; i < skeletonNodes.Count; i++)
                {
                    var boneName = Animation.CleanBoneAndStripBase(skeleton.BoneMapping[i], skeleton.ModelBase);
                    if (!poseArray.TryGetValue(boneName, out var trackFragment)) continue;//return?

                    var boneTransform = trackFragment.TrackDefFragment.Frames[frame];
                    if (boneTransform == null) continue; //return?

                    skeletonNodes[i]
                        .WithLocalScale(new Vector3(boneTransform.Scale, boneTransform.Scale, boneTransform.Scale))
                        .WithLocalRotation(new Quaternion()
                        {
                            X = (float)(boneTransform.Rotation.x * Math.PI * -1)/180,
                            Y = (float)(boneTransform.Rotation.z * Math.PI * -1)/180,
                            Z = (float)(boneTransform.Rotation.y * Math.PI * -1)/180,
                            W = (float)(boneTransform.Rotation.w * Math.PI)/180
                        })
                        .WithLocalTranslation(boneTransform.Translation.ToVector3(true));
                }
            }
        }
        public void AddCombinedMeshToScene(string skeletonModelBase = null, ObjectInstance objectInstance = null, string meshName = null)
        {
            IMeshBuilder<MaterialBuilder> combinedMesh;
            if (meshName != null && _sharedMeshes.TryGetValue(meshName, out var existingMesh))
            {
                combinedMesh = existingMesh;
            }
            else
            {
                combinedMesh = _combinedMeshBuilder;
            }
            if (combinedMesh == null) return;
            var worldTransformMatrix = Matrix4x4.Identity;
            if (objectInstance != null)
            {
                worldTransformMatrix *= CreateTransformMatrixForObjectInstance(objectInstance);
            }

            if (skeletonModelBase == null || !_skeletons.TryGetValue(skeletonModelBase, out var skeleton))
            {
                _scene.AddRigidMesh(combinedMesh, worldTransformMatrix * CorrectedWorldMatrix);
            }
            else
            {
                _scene.AddSkinnedMesh(combinedMesh, worldTransformMatrix, skeleton.ToArray());
            }

            if (meshName != null && !_sharedMeshes.ContainsKey(meshName))
            {
                _sharedMeshes.Add(meshName, combinedMesh);
            }
            _combinedMeshBuilder = null;
        }

        public override void WriteAssetToFile(string fileName)
        {
            WriteAssetToFile(fileName, false);
        }

        public void WriteAssetToFile(string fileName, bool useExistingImages, string skeletonModelBase = null)
        {
            AddCombinedMeshToScene(skeletonModelBase);
            var outputFilePath = FixFilePath(fileName);
            var model = _scene.ToGltf2();
            if (_exportFormat == GltfExportFormat.GlTF)
            {
                if (!useExistingImages)
                {
                    model.SaveGLTF(outputFilePath);
                    return;
                }
                var writeSettings = new SharpGLTF.Schema2.WriteSettings()
                {
                    JsonIndented = true,
                    ImageWriteCallback = (context, uri, image) =>
                    {
                        var imageSourcePath = image.SourcePath;
                        return $"Textures/{Path.GetFileName(imageSourcePath)}";
                    }
                };

                model.SaveGLTF(outputFilePath, writeSettings);
            }
            else // Glb
            {
                model.SaveGLB(outputFilePath);
            }
        }
        public override void ClearExportData()
        {
            _scene = null;
            _scene = new SceneBuilder();
            Materials.Clear();
            _sharedMeshes.Clear();
            _skeletons.Clear();
        }

        public new int GetExportByteCount() => 0;

        private Matrix4x4 CreateTransformMatrixForObjectInstance(ObjectInstance instance)
        {
            var transformMatrix = Matrix4x4.CreateScale(instance.Scale.ToVector3())
                * Matrix4x4.CreateFromYawPitchRoll(
                    (float)(instance.Rotation.z * Math.PI)/180,
                    (float)(instance.Rotation.x * Math.PI)/180,
                    (float)(instance.Rotation.y * Math.PI)/180
                )
                * Matrix4x4.CreateTranslation(instance.Position.ToVector3(true));
            return transformMatrix;
        }

        private string GetMaterialName(Material eqMaterial)
        {
            return $"{MaterialList.GetMaterialPrefix(eqMaterial.ShaderType)}{eqMaterial.GetFirstBitmapNameWithoutExtension()}";
        }

        private MaterialBuilder GetBlankMaterial()
        {
            return new MaterialBuilder(MaterialBlankName)
                .WithDoubleSide(false)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 1, 0));
        }

        private MaterialBuilder GetInvisibleMaterial()
        {
            return new MaterialBuilder(MaterialInvisName)
                .WithDoubleSide(false)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 1, 1))
                .WithAlpha(AlphaMode.MASK);
        }

        private IMeshBuilder<MaterialBuilder> InstantiateMeshBuilder(string meshName, bool isSkinned = false, bool canExportVertexColors = false)
        {
            var meshBuilderType = typeof(MeshBuilder<,,>).MakeGenericType(
                typeof(VertexPositionNormal),
                canExportVertexColors ? typeof(VertexColor1Texture1) : typeof(VertexTexture1),
                isSkinned ? typeof(VertexJoints4) : typeof(VertexEmpty));

            return (IMeshBuilder<MaterialBuilder>) Activator.CreateInstance(meshBuilderType, meshName);

        }

        private (Vector4 v0, Vector4 v1, Vector4 v2) GetVertexColorVectors(Mesh mesh, 
            (int v0, int v1, int v2) vertexIndices, ObjectInstance objectInstance = null)
        {
            var objInstanceColors = objectInstance?.Colors?.Colors ?? new List<Color>();
            var meshColors = mesh?.Colors ?? new List<Color>();

            var v0Color = vertexIndices.v0 < objInstanceColors.Count ? objInstanceColors[vertexIndices.v0].ToVector4() :
                vertexIndices.v0 < meshColors.Count ? meshColors[vertexIndices.v0].ToVector4() :
                new Vector4(0f, 0f, 0f, 0f);
            var v1Color = vertexIndices.v1 < objInstanceColors.Count ? objInstanceColors[vertexIndices.v1].ToVector4() :
                vertexIndices.v1 < meshColors.Count ? meshColors[vertexIndices.v1].ToVector4() :
                new Vector4(0f, 0f, 0f, 0f);
            var v2Color = vertexIndices.v2 < objInstanceColors.Count ? objInstanceColors[vertexIndices.v2].ToVector4() :
                vertexIndices.v2 < meshColors.Count ? meshColors[vertexIndices.v2].ToVector4() :
                new Vector4(0f, 0f, 0f, 0f);

            return (v0Color, v1Color, v2Color);
        }

        private List<NodeBuilder> AddNewSkeleton(SkeletonHierarchy skeleton)
        {
            var skeletonNodes = new List<NodeBuilder>();
            var duplicateNameDictionary = new Dictionary<string, int>();
            foreach (var bone in skeleton.Skeleton)
            {
                var boneName = bone.CleanedName;
                if (duplicateNameDictionary.TryGetValue(boneName, out var count))
                {
                    skeletonNodes.Add(new NodeBuilder($"{boneName}_{count:00}"));
                    duplicateNameDictionary[boneName] = ++count;
                }
                else
                {
                    skeletonNodes.Add(new NodeBuilder(boneName));
                    duplicateNameDictionary.Add(boneName, 0);
                }
            }
            for (var i = 0; i < skeletonNodes.Count; i++)
            {
                var node = skeletonNodes[i];
                var bone = skeleton.Skeleton[i];
                bone.Children.ForEach(b => node.AddNode(skeletonNodes[b]));
            }
            _skeletons.Add(skeleton.ModelBase, skeletonNodes);
            return skeletonNodes;
        }

        private int GetBoneIndexForVertex(Mesh mesh, int vertexIndex)
        {
            foreach (var indexedMobVertexPiece in mesh.MobPieces)
            {
                if (vertexIndex >= indexedMobVertexPiece.Value.Start &&
                    vertexIndex < indexedMobVertexPiece.Value.Start + indexedMobVertexPiece.Value.Count)
                {
                    return indexedMobVertexPiece.Key;
                }
            }
            return 0;
        }

        private string FixFilePath(string filePath)
        {
            var fixedExtension = _exportFormat == GltfExportFormat.GlTF ? ".gltf" : ".glb";
            return Path.ChangeExtension(filePath, fixedExtension);
        }
    }

    static class VectorConversionExtensionMethods
    {
        public static Vector2 ToVector2(this vec2 v2, bool negateY = false)
        {
            var y = negateY ? -v2.y : v2.y;
            return new Vector2(v2.x, y);
        }

        public static Vector3 ToVector3(this vec3 v3, bool swapYandZ = false)
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

        public static Vector4 ToVector4(this Color color)
        {
            return new Vector4(color.R, color.G, color.B, color.A);
        }
    }


}
