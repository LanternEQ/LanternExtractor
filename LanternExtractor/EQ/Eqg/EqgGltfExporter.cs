using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LanternExtractor.EQ.Eqg.DataTypes;
using LanternExtractor.Infrastructure.Logger;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;

namespace LanternExtractor.EQ.Eqg
{
    /// <summary>
    /// Exports EQG models, skeletons, animations and zones to glTF.
    /// EQG model data is right handed and Z up. glTF is right handed and Y up, so every model is
    /// placed under a root node that rotates it -90 degrees around X. Zone placements already live
    /// in a Y up space (the placement rotation stands each Z up model upright), so zone object
    /// transforms are used directly.
    /// </summary>
    public class EqgGltfExporter
    {
        private static readonly Matrix4x4 ZUpToYUp = Matrix4x4.CreateRotationX(-(float)Math.PI / 2f);
        private const float MaterialRoughness = 0.9f;
        private const string BlankMaterialName = "Blank";

        private readonly string _textureFolder;
        private readonly bool _exportVertexColors;
        private readonly bool _exportAnimations;
        private readonly bool _exportGlb;
        private readonly ILogger _logger;

        private readonly Dictionary<string, MaterialBuilder> _materials =
            new Dictionary<string, MaterialBuilder>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _missingTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public EqgGltfExporter(string textureFolder, Settings settings, ILogger logger)
        {
            _textureFolder = textureFolder;
            _exportVertexColors = settings.ExportGltfVertexColors;
            _exportAnimations = settings.ExportAllAnimationFrames;
            _exportGlb = settings.ExportGltfInGlbFormat;
            _logger = logger;
        }

        public string FileExtension => _exportGlb ? ".glb" : ".gltf";

        /// <summary>
        /// Exports a single model. Models with weighted bones are exported as skinned meshes with an
        /// armature and any matching animations. Other models are exported as rigid meshes, with their
        /// bones (usually attachment points like ROOT_PROP01) kept as empty nodes.
        /// </summary>
        public void ExportModel(EqgModel model, IList<EqgAnimation> animations, string outputPath,
            string texturePathPrefix)
        {
            var scene = new SceneBuilder();
            var root = new NodeBuilder(model.Name) { LocalMatrix = ZUpToYUp };
            var boneNodes = CreateBoneNodes(model, root);

            if (model.IsSkinned())
            {
                foreach (var piece in model.Pieces)
                {
                    var mesh = BuildMesh(model, piece, true);
                    if (mesh != null)
                    {
                        scene.AddSkinnedMesh(mesh, root.WorldMatrix, boneNodes.ToArray());
                    }
                }

                if (_exportAnimations && animations != null)
                {
                    foreach (var animation in animations)
                    {
                        ApplyAnimation(model, boneNodes, animation);
                    }
                }
            }
            else
            {
                foreach (var piece in model.Pieces)
                {
                    var mesh = BuildMesh(model, piece, false);
                    if (mesh == null)
                    {
                        continue;
                    }

                    var pieceNode = new NodeBuilder(piece.Name);
                    root.AddNode(pieceNode);
                    scene.AddRigidMesh(mesh, pieceNode);
                }
            }

            Write(scene, outputPath, texturePathPrefix);
        }

        /// <summary>
        /// Exports the terrain and (optionally) the placed objects of a zone into a single scene
        /// </summary>
        public void ExportZone(EqgZone zone, IDictionary<string, EqgModel> models, bool includeObjects,
            string outputPath, string texturePathPrefix)
        {
            var scene = new SceneBuilder();
            var meshCache = new Dictionary<string, List<IMeshBuilder<MaterialBuilder>>>(
                StringComparer.OrdinalIgnoreCase);
            var missingModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var placementCount = 0;

            foreach (var placement in zone.Placements)
            {
                if (string.IsNullOrEmpty(placement.ModelFileName))
                {
                    continue;
                }

                if (!models.TryGetValue(placement.ModelFileName, out var model))
                {
                    missingModels.Add(placement.ModelFileName);
                    continue;
                }

                if (model.Type != EqgModelType.Ter && !includeObjects)
                {
                    continue;
                }

                if (!meshCache.TryGetValue(placement.ModelFileName, out var meshes))
                {
                    meshes = model.Pieces.Select(p => BuildMesh(model, p, false)).Where(m => m != null).ToList();
                    meshCache[placement.ModelFileName] = meshes;
                }

                var transform = placement.GetTransform();

                foreach (var mesh in meshes)
                {
                    var node = new NodeBuilder($"{placement.Name}.{placementCount:0000}") { LocalMatrix = transform };
                    scene.AddRigidMesh(mesh, node);
                }

                placementCount++;
            }

            foreach (var missingModel in missingModels)
            {
                _logger.LogWarning($"EqgGltfExporter: Zone {zone.Name} references missing model {missingModel}");
            }

            if (placementCount == 0)
            {
                _logger.LogWarning($"EqgGltfExporter: Zone {zone.Name} has no exportable placements");
                return;
            }

            Write(scene, outputPath, texturePathPrefix);
        }

        private static List<NodeBuilder> CreateBoneNodes(EqgModel model, NodeBuilder root)
        {
            var nodes = new List<NodeBuilder>();
            var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var bone in model.Bones)
            {
                var name = string.IsNullOrEmpty(bone.Name) ? "bone" : bone.Name;
                if (usedNames.TryGetValue(name, out var count))
                {
                    usedNames[name] = count + 1;
                    name = $"{name}_{count:00}";
                }
                else
                {
                    usedNames[name] = 1;
                }

                var node = new NodeBuilder(name)
                    .WithLocalScale(bone.Scale)
                    .WithLocalRotation(Quaternion.Normalize(bone.Rotation))
                    .WithLocalTranslation(bone.Translation);
                nodes.Add(node);
            }

            for (var i = 0; i < model.Bones.Count; i++)
            {
                var parent = model.Bones[i].Parent;
                if (parent >= 0)
                {
                    nodes[parent].AddNode(nodes[i]);
                }
                else
                {
                    root.AddNode(nodes[i]);
                }
            }

            return nodes;
        }

        private void ApplyAnimation(EqgModel model, List<NodeBuilder> boneNodes, EqgAnimation animation)
        {
            var tracksByBone = new Dictionary<string, EqgAnimationTrack>(StringComparer.OrdinalIgnoreCase);
            foreach (var track in animation.Tracks)
            {
                tracksByBone[track.BoneName] = track;
            }

            for (var i = 0; i < model.Bones.Count; i++)
            {
                var bone = model.Bones[i];
                var node = boneNodes[i];

                if (!tracksByBone.TryGetValue(bone.Name, out var track) || track.Frames.Count == 0)
                {
                    // Key unanimated bones at their bind pose so switching between animations
                    // in a DCC tool does not leave bones in the pose of a previous animation
                    node.UseTranslation(animation.Name).WithPoint(0f, bone.Translation);
                    node.UseRotation(animation.Name).WithPoint(0f, Quaternion.Normalize(bone.Rotation));
                    node.UseScale(animation.Name).WithPoint(0f, bone.Scale);
                    continue;
                }

                var previousTime = -1;
                var previousRotation = Quaternion.Identity;
                foreach (var frame in track.Frames)
                {
                    // glTF requires strictly increasing keyframe times
                    if (frame.TimeMs <= previousTime)
                    {
                        continue;
                    }

                    // Keep consecutive quaternions in the same hemisphere so interpolation takes the short path
                    var rotation = Quaternion.Normalize(frame.Rotation);
                    if (previousTime >= 0 && Quaternion.Dot(previousRotation, rotation) < 0f)
                    {
                        rotation = Quaternion.Negate(rotation);
                    }

                    var time = frame.TimeMs / 1000f;
                    node.UseTranslation(animation.Name).WithPoint(time, frame.Translation);
                    node.UseRotation(animation.Name).WithPoint(time, rotation);
                    node.UseScale(animation.Name).WithPoint(time, frame.Scale);

                    previousTime = frame.TimeMs;
                    previousRotation = rotation;
                }
            }
        }

        private IMeshBuilder<MaterialBuilder> BuildMesh(EqgModel model, EqgMeshPiece piece, bool skinned)
        {
            var useColors = _exportVertexColors && piece.HasVertexColors;
            var meshBuilderType = typeof(MeshBuilder<,,>).MakeGenericType(
                typeof(VertexPositionNormal),
                useColors ? typeof(VertexColor1Texture1) : typeof(VertexTexture1),
                skinned ? typeof(VertexJoints4) : typeof(VertexEmpty));
            var mesh = (IMeshBuilder<MaterialBuilder>)Activator.CreateInstance(meshBuilderType, piece.Name);

            var triangleCount = 0;
            var vertexCount = piece.Vertices.Count;

            foreach (var triangle in piece.Triangles)
            {
                if (triangle.V0 < 0 || triangle.V0 >= vertexCount || triangle.V1 < 0 || triangle.V1 >= vertexCount ||
                    triangle.V2 < 0 || triangle.V2 >= vertexCount)
                {
                    continue;
                }

                var material = GetMaterial(model, triangle.MaterialIndex);
                var primitive = mesh.UsePrimitive(material);
                primitive.AddTriangle(
                    CreateVertex(piece, triangle.V0, useColors, skinned, model.Bones.Count),
                    CreateVertex(piece, triangle.V1, useColors, skinned, model.Bones.Count),
                    CreateVertex(piece, triangle.V2, useColors, skinned, model.Bones.Count));
                triangleCount++;
            }

            return triangleCount == 0 ? null : mesh;
        }

        private static IVertexBuilder CreateVertex(EqgMeshPiece piece, int index, bool useColors, bool skinned,
            int boneCount)
        {
            var vertex = piece.Vertices[index];
            var normal = vertex.Normal.LengthSquared() > 0f ? Vector3.Normalize(vertex.Normal) : Vector3.UnitZ;
            var geometry = new VertexPositionNormal(vertex.Position, normal);

            if (!skinned)
            {
                if (useColors)
                {
                    return new VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexEmpty>(
                        geometry, new VertexColor1Texture1(vertex.Color, vertex.Uv));
                }

                return new VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>(
                    geometry, new VertexTexture1(vertex.Uv));
            }

            var joints = GetJoints(piece, index, boneCount);
            if (useColors)
            {
                return new VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints4>(
                    geometry, new VertexColor1Texture1(vertex.Color, vertex.Uv), joints);
            }

            return new VertexBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4>(
                geometry, new VertexTexture1(vertex.Uv), joints);
        }

        private static VertexJoints4 GetJoints(EqgMeshPiece piece, int index, int boneCount)
        {
            if (piece.Weights == null || index >= piece.Weights.Length)
            {
                return new VertexJoints4(0);
            }

            var w = piece.Weights[index];
            var bindings = new List<(int, float)>(4);
            AddBinding(bindings, w.Bone0, w.Weight0, boneCount);
            AddBinding(bindings, w.Bone1, w.Weight1, boneCount);
            AddBinding(bindings, w.Bone2, w.Weight2, boneCount);
            AddBinding(bindings, w.Bone3, w.Weight3, boneCount);

            if (bindings.Count == 0)
            {
                return new VertexJoints4(0);
            }

            var total = bindings.Sum(b => b.Item2);
            return new VertexJoints4(bindings.Select(b => (b.Item1, b.Item2 / total)).ToArray());
        }

        private static void AddBinding(List<(int, float)> bindings, int bone, float weight, int boneCount)
        {
            if (weight > 0f && bone >= 0 && bone < boneCount)
            {
                bindings.Add((bone, weight));
            }
        }

        private MaterialBuilder GetMaterial(EqgModel model, int materialIndex)
        {
            var eqgMaterial = materialIndex >= 0 && materialIndex < model.Materials.Count
                ? model.Materials[materialIndex]
                : null;
            var materialName = eqgMaterial?.ExportName;

            if (materialName == null)
            {
                return GetBlankMaterial();
            }

            var texture = eqgMaterial.DiffuseTexture;
            var textureName = eqgMaterial.DiffuseTextureBaseName;
            var alphaMode = eqgMaterial.IsAlphaBlended ? AlphaMode.BLEND
                : eqgMaterial.IsAlphaMasked ? AlphaMode.MASK
                : AlphaMode.OPAQUE;

            if (_materials.TryGetValue(materialName, out var existing))
            {
                return existing;
            }

            var imagePath = Path.Combine(_textureFolder, textureName + ".png");
            if (!File.Exists(imagePath))
            {
                if (_missingTextures.Add(textureName))
                {
                    _logger.LogWarning($"EqgGltfExporter: Missing texture {texture} for material {eqgMaterial.Name} " +
                                       $"in {model.FileName}. Using blank material.");
                }

                return GetBlankMaterial();
            }

            var material = new MaterialBuilder(materialName)
                .WithDoubleSide(false)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, MaterialRoughness)
                .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, 0f);
            material.UseChannel(KnownChannel.BaseColor)
                .UseTexture()
                .WithPrimaryImage(ImageBuilder.From(new MemoryImage(imagePath), textureName));

            if (alphaMode == AlphaMode.MASK)
            {
                material.WithAlpha(AlphaMode.MASK, 0.5f);
            }
            else
            {
                material.WithAlpha(alphaMode);
            }

            _materials[materialName] = material;
            return material;
        }

        private MaterialBuilder GetBlankMaterial()
        {
            if (!_materials.TryGetValue(BlankMaterialName, out var material))
            {
                material = new MaterialBuilder(BlankMaterialName)
                    .WithDoubleSide(false)
                    .WithMetallicRoughnessShader()
                    .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 1, 1));
                _materials[BlankMaterialName] = material;
            }

            return material;
        }

        private void Write(SceneBuilder scene, string outputPath, string texturePathPrefix)
        {
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var model = scene.ToGltf2();
            if (_exportGlb)
            {
                model.SaveGLB(outputPath);
                return;
            }

            // Reference the PNGs already written to the textures folder rather than duplicating them
            var writeSettings = new SharpGLTF.Schema2.WriteSettings
            {
                JsonIndented = true,
                ImageWriteCallback = (context, uri, image) =>
                    $"{texturePathPrefix}{Path.GetFileName(image.SourcePath)}"
            };
            model.SaveGLTF(outputPath, writeSettings);
        }
    }
}
