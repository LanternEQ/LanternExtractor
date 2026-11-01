using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using LanternExtractor.EQ.Eqg.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Eqg
{
    /// <summary>
    /// Exports EQG models and zones to OBJ/MTL. Models are rotated from Z up to Y up to match the glTF export.
    /// OBJ does not support skeletons, so skinned models are written in their bind pose, and optionally
    /// as one posed OBJ per animation frame ({model}_{animation}_{frame}.obj) like the S3D OBJ export.
    /// </summary>
    public class EqgObjExporter
    {
        private static readonly Matrix4x4 ZUpToYUp = Matrix4x4.CreateRotationX(-(float)Math.PI / 2f);
        private static readonly CultureInfo Format = CultureInfo.InvariantCulture;
        private const string BlankMaterialName = "Blank";

        private readonly string _textureFolder;
        private readonly ILogger _logger;

        private class ObjInstance
        {
            public EqgModel Model;
            public EqgMeshPiece Piece;
            public Matrix4x4 Transform;
            public string Name;
            public Vector3[] Positions;
            public Vector3[] Normals;
        }

        public EqgObjExporter(string textureFolder, ILogger logger)
        {
            _textureFolder = textureFolder;
            _logger = logger;
        }

        public void ExportModel(EqgModel model, IList<EqgAnimation> animations, bool exportAnimationFrames,
            string outputPath, string texturePathPrefix)
        {
            var instances = model.Pieces.Select(p => new ObjInstance
            {
                Model = model,
                Piece = p,
                Transform = ZUpToYUp,
                Name = p.Name
            }).ToList();

            var mtlFileName = Path.GetFileNameWithoutExtension(outputPath) + LanternStrings.FormatMtlExtension;
            if (!Write(instances, outputPath, mtlFileName, texturePathPrefix, true))
            {
                return;
            }

            if (!exportAnimationFrames || animations == null || animations.Count == 0 || !model.IsSkinned())
            {
                return;
            }

            var directory = Path.GetDirectoryName(outputPath) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(outputPath);
            var bindWorld = model.GetBoneWorldMatrices();
            var inverseBind = bindWorld.Select(m => Matrix4x4.Invert(m, out var inverse) ? inverse : Matrix4x4.Identity)
                .ToArray();

            foreach (var animation in animations)
            {
                var frameCount = animation.Tracks.Count == 0 ? 0 : animation.Tracks.Max(t => t.Frames.Count);
                for (var frame = 0; frame < frameCount; frame++)
                {
                    var skinMatrices = GetSkinMatrices(model, animation, frame, inverseBind);
                    foreach (var instance in instances)
                    {
                        PoseInstance(instance, skinMatrices);
                    }

                    var framePath = Path.Combine(directory, $"{baseName}_{animation.Name}_{frame}.obj");
                    Write(instances, framePath, mtlFileName, texturePathPrefix, false);
                }
            }
        }

        /// <summary>
        /// Exports the terrain and (optionally) the placed objects of a zone into a single OBJ
        /// </summary>
        public void ExportZone(EqgZone zone, IDictionary<string, EqgModel> models, bool includeObjects,
            string outputPath, string texturePathPrefix)
        {
            var instances = new List<ObjInstance>();
            var placementIndex = 0;

            foreach (var placement in zone.Placements)
            {
                if (string.IsNullOrEmpty(placement.ModelFileName) ||
                    !models.TryGetValue(placement.ModelFileName, out var model))
                {
                    continue;
                }

                if (model.Type != EqgModelType.Ter && !includeObjects)
                {
                    continue;
                }

                var transform = placement.GetTransform();
                foreach (var piece in model.Pieces)
                {
                    instances.Add(new ObjInstance
                    {
                        Model = model,
                        Piece = piece,
                        Transform = transform,
                        Name = $"{placement.Name}.{placementIndex:0000}"
                    });
                }

                placementIndex++;
            }

            if (instances.Count == 0)
            {
                _logger.LogWarning($"EqgObjExporter: Zone {zone.Name} has no exportable placements");
                return;
            }

            var mtlFileName = Path.GetFileNameWithoutExtension(outputPath) + LanternStrings.FormatMtlExtension;
            Write(instances, outputPath, mtlFileName, texturePathPrefix, true);
        }

        /// <summary>
        /// Calculates the matrix that moves each bone from its bind pose to its pose in the given frame
        /// </summary>
        private static Matrix4x4[] GetSkinMatrices(EqgModel model, EqgAnimation animation, int frame,
            Matrix4x4[] inverseBind)
        {
            var tracks = new Dictionary<string, EqgAnimationTrack>(StringComparer.OrdinalIgnoreCase);
            foreach (var track in animation.Tracks)
            {
                tracks[track.BoneName] = track;
            }

            var locals = new Matrix4x4[model.Bones.Count];
            for (var i = 0; i < model.Bones.Count; i++)
            {
                var bone = model.Bones[i];
                if (tracks.TryGetValue(bone.Name, out var track) && track.Frames.Count > 0)
                {
                    var key = track.Frames[Math.Min(frame, track.Frames.Count - 1)];
                    locals[i] = EqgModel.CreateBoneMatrix(key.Translation, key.Rotation, key.Scale);
                }
                else
                {
                    locals[i] = EqgModel.CreateBoneMatrix(bone.Translation, bone.Rotation, bone.Scale);
                }
            }

            var world = model.GetBoneWorldMatrices(locals);
            for (var i = 0; i < world.Length; i++)
            {
                world[i] = inverseBind[i] * world[i];
            }

            return world;
        }

        private static void PoseInstance(ObjInstance instance, Matrix4x4[] skinMatrices)
        {
            var piece = instance.Piece;
            instance.Positions = new Vector3[piece.Vertices.Count];
            instance.Normals = new Vector3[piece.Vertices.Count];

            for (var i = 0; i < piece.Vertices.Count; i++)
            {
                var vertex = piece.Vertices[i];
                if (piece.Weights == null || i >= piece.Weights.Length)
                {
                    instance.Positions[i] = vertex.Position;
                    instance.Normals[i] = vertex.Normal;
                    continue;
                }

                var w = piece.Weights[i];
                var position = Vector3.Zero;
                var normal = Vector3.Zero;
                var total = 0f;
                Accumulate(skinMatrices, vertex, w.Bone0, w.Weight0, ref position, ref normal, ref total);
                Accumulate(skinMatrices, vertex, w.Bone1, w.Weight1, ref position, ref normal, ref total);
                Accumulate(skinMatrices, vertex, w.Bone2, w.Weight2, ref position, ref normal, ref total);
                Accumulate(skinMatrices, vertex, w.Bone3, w.Weight3, ref position, ref normal, ref total);

                instance.Positions[i] = total > 0f ? position / total : vertex.Position;
                instance.Normals[i] = total > 0f ? normal : vertex.Normal;
            }
        }

        private static void Accumulate(Matrix4x4[] skinMatrices, EqgVertex vertex, int bone, float weight,
            ref Vector3 position, ref Vector3 normal, ref float total)
        {
            if (weight <= 0f || bone < 0 || bone >= skinMatrices.Length)
            {
                return;
            }

            position += Vector3.Transform(vertex.Position, skinMatrices[bone]) * weight;
            normal += Vector3.TransformNormal(vertex.Normal, skinMatrices[bone]) * weight;
            total += weight;
        }

        /// <summary>
        /// Writes the OBJ and, if requested, its MTL. Returns false if there was no geometry to write.
        /// </summary>
        private bool Write(List<ObjInstance> instances, string outputPath, string mtlFileName,
            string texturePathPrefix, bool writeMtl)
        {
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var usedMaterials = new Dictionary<string, EqgMaterial>(StringComparer.OrdinalIgnoreCase);
            var vertexOffset = 1;
            var wroteFaces = false;

            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine(LanternStrings.ExportHeaderTitle + "EQG OBJ");
                writer.WriteLine(LanternStrings.ObjMaterialHeader + mtlFileName);

                foreach (var instance in instances)
                {
                    var piece = instance.Piece;
                    var vertexCount = piece.Vertices.Count;
                    var validTriangles = piece.Triangles.Where(t =>
                        t.V0 >= 0 && t.V0 < vertexCount && t.V1 >= 0 && t.V1 < vertexCount &&
                        t.V2 >= 0 && t.V2 < vertexCount).ToList();

                    if (validTriangles.Count == 0)
                    {
                        continue;
                    }

                    writer.WriteLine("g " + instance.Name);

                    for (var i = 0; i < vertexCount; i++)
                    {
                        var vertex = piece.Vertices[i];
                        var sourcePosition = instance.Positions?[i] ?? vertex.Position;
                        var sourceNormal = instance.Normals?[i] ?? vertex.Normal;
                        var position = Vector3.Transform(sourcePosition, instance.Transform);
                        var normal = Vector3.TransformNormal(sourceNormal, instance.Transform);
                        normal = normal.LengthSquared() > 0f ? Vector3.Normalize(normal) : Vector3.UnitY;

                        writer.WriteLine(string.Format(Format, "v {0:0.######} {1:0.######} {2:0.######}",
                            position.X, position.Y, position.Z));
                        writer.WriteLine(string.Format(Format, "vt {0:0.######} {1:0.######}",
                            vertex.Uv.X, 1f - vertex.Uv.Y));
                        writer.WriteLine(string.Format(Format, "vn {0:0.######} {1:0.######} {2:0.######}",
                            normal.X, normal.Y, normal.Z));
                    }

                    string currentMaterial = null;
                    foreach (var triangle in validTriangles.OrderBy(t => t.MaterialIndex))
                    {
                        var material = triangle.MaterialIndex >= 0 &&
                                       triangle.MaterialIndex < instance.Model.Materials.Count
                            ? instance.Model.Materials[triangle.MaterialIndex]
                            : null;
                        var materialName = material?.ExportName ?? BlankMaterialName;

                        if (materialName != currentMaterial)
                        {
                            writer.WriteLine(LanternStrings.ObjUseMtlPrefix + materialName);
                            currentMaterial = materialName;
                            if (!usedMaterials.ContainsKey(materialName))
                            {
                                usedMaterials[materialName] = material;
                            }
                        }

                        var a = triangle.V0 + vertexOffset;
                        var b = triangle.V1 + vertexOffset;
                        var c = triangle.V2 + vertexOffset;
                        writer.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
                    }

                    vertexOffset += vertexCount;
                    wroteFaces = true;
                }
            }

            if (!wroteFaces)
            {
                File.Delete(outputPath);
                return false;
            }

            if (writeMtl)
            {
                WriteMtl(Path.Combine(outputDirectory ?? string.Empty, mtlFileName), usedMaterials, texturePathPrefix);
            }

            return true;
        }

        private void WriteMtl(string path, Dictionary<string, EqgMaterial> materials, string texturePathPrefix)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(LanternStrings.ExportHeaderTitle + "EQG MTL");

                foreach (var entry in materials.OrderBy(m => m.Key, StringComparer.OrdinalIgnoreCase))
                {
                    writer.WriteLine(LanternStrings.ObjNewMaterialPrefix + " " + entry.Key);
                    writer.WriteLine("Ka 1.000 1.000 1.000");
                    writer.WriteLine("Kd 1.000 1.000 1.000");
                    writer.WriteLine("Ks 0.000 0.000 0.000");
                    writer.WriteLine("d 1.0");
                    writer.WriteLine("illum 2");

                    var textureName = entry.Value?.DiffuseTextureBaseName;
                    if (textureName != null && File.Exists(Path.Combine(_textureFolder, textureName + ".png")))
                    {
                        var texturePath = $"{texturePathPrefix}{textureName}.png";
                        writer.WriteLine("map_Kd " + texturePath);

                        // Transparent shaders take their alpha from the diffuse texture
                        if (entry.Value.IsAlphaBlended || entry.Value.IsAlphaMasked)
                        {
                            writer.WriteLine("map_d " + texturePath);
                        }
                    }

                    writer.WriteLine();
                }
            }
        }
    }
}
