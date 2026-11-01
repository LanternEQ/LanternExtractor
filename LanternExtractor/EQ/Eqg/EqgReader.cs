using System;
using System.IO;
using System.Numerics;
using System.Text;
using LanternExtractor.EQ.Eqg.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Eqg
{
    /// <summary>
    /// Parses the binary file formats found inside EQG archives.
    /// All formats share a header of magic, version and string table size, followed by
    /// format specific counts and a string table that other fields reference by offset.
    /// </summary>
    public static class EqgReader
    {
        private const string ModelMagic = "EQGM";
        private const string SkinnedModelMagic = "EQGS";
        private const string TerrainMagic = "EQGT";
        private const string AnimationMagic = "EQGA";
        private const string ZoneMagic = "EQGZ";

        public static string GetMagic(byte[] bytes)
        {
            return bytes == null || bytes.Length < 4 ? string.Empty : Encoding.ASCII.GetString(bytes, 0, 4);
        }

        /// <summary>
        /// Reads a MOD (EQGM), TER (EQGT) or MDS (EQGS) file
        /// </summary>
        public static EqgModel ReadModel(byte[] bytes, string fileName, ILogger logger)
        {
            try
            {
                switch (GetMagic(bytes))
                {
                    case ModelMagic:
                        return ReadModOrTer(bytes, fileName, EqgModelType.Mod);
                    case TerrainMagic:
                        return ReadModOrTer(bytes, fileName, EqgModelType.Ter);
                    case SkinnedModelMagic:
                        return ReadMds(bytes, fileName);
                    default:
                        logger.LogWarning($"EqgReader: Unsupported model format in {fileName}");
                        return null;
                }
            }
            catch (Exception e) when (e is EndOfStreamException || e is ArgumentException ||
                                      e is IndexOutOfRangeException)
            {
                logger.LogError($"EqgReader: Failed to parse model {fileName}: {e.Message}");
                return null;
            }
        }

        public static EqgAnimation ReadAnimation(byte[] bytes, string fileName, ILogger logger)
        {
            if (GetMagic(bytes) != AnimationMagic)
            {
                logger.LogWarning($"EqgReader: Unsupported animation format in {fileName}");
                return null;
            }

            try
            {
                var reader = new BinaryReader(new MemoryStream(bytes));
                reader.ReadUInt32();
                var version = reader.ReadUInt32();
                var stringTableSize = reader.ReadUInt32();
                var boneCount = reader.ReadUInt32();

                // Version 2 adds an extra header field, which is always 0 in the client files
                if (version > 1)
                {
                    reader.ReadUInt32();
                }

                var strings = reader.ReadBytes((int)stringTableSize);
                var animation = new EqgAnimation
                {
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    FileName = fileName
                };

                for (var i = 0; i < boneCount; i++)
                {
                    var frameCount = reader.ReadUInt32();
                    var track = new EqgAnimationTrack { BoneName = GetString(strings, reader.ReadInt32()) };

                    for (var j = 0; j < frameCount; j++)
                    {
                        var timeMs = reader.ReadInt32();
                        var translation = ReadVector3(reader);
                        var rotation = ReadQuaternion(reader);
                        var scale = ReadVector3(reader);
                        SanitizeTransform(ref translation, ref rotation, ref scale);
                        track.Frames.Add(new EqgKeyframe
                        {
                            TimeMs = timeMs,
                            Translation = translation,
                            Rotation = rotation,
                            Scale = scale
                        });
                    }

                    animation.Tracks.Add(track);
                }

                return animation;
            }
            catch (Exception e) when (e is EndOfStreamException || e is ArgumentException)
            {
                logger.LogError($"EqgReader: Failed to parse animation {fileName}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads a binary ZON file. Version 4 zones (EQTZP) are text based terrain zones and are not supported.
        /// </summary>
        public static EqgZone ReadZone(byte[] bytes, string fileName, ILogger logger)
        {
            if (GetMagic(bytes) != ZoneMagic)
            {
                logger.LogWarning($"EqgReader: Unsupported zone format in {fileName} (only EQGZ v1 is supported)");
                return null;
            }

            try
            {
                var reader = new BinaryReader(new MemoryStream(bytes));
                reader.ReadUInt32();
                var version = reader.ReadUInt32();
                if (version != 1)
                {
                    logger.LogWarning($"EqgReader: Unsupported zone version {version} in {fileName}");
                    return null;
                }

                var stringTableSize = reader.ReadUInt32();
                var modelCount = reader.ReadUInt32();
                var placementCount = reader.ReadUInt32();
                var regionCount = reader.ReadUInt32();
                var lightCount = reader.ReadUInt32();
                var strings = reader.ReadBytes((int)stringTableSize);

                var zone = new EqgZone { Name = Path.GetFileNameWithoutExtension(fileName) };

                for (var i = 0; i < modelCount; i++)
                {
                    zone.ModelFileNames.Add(GetString(strings, reader.ReadInt32()));
                }

                for (var i = 0; i < placementCount; i++)
                {
                    var modelIndex = reader.ReadInt32();
                    var placement = new EqgPlacement
                    {
                        ModelFileName = modelIndex >= 0 && modelIndex < zone.ModelFileNames.Count
                            ? zone.ModelFileNames[modelIndex]
                            : null,
                        Name = GetString(strings, reader.ReadInt32()),
                        Position = ReadVector3(reader),
                        Rotation = ReadVector3(reader),
                        Scale = reader.ReadSingle()
                    };
                    zone.Placements.Add(placement);
                }

                // Regions: name, center, unknown, extents
                reader.BaseStream.Position += regionCount * 40;

                for (var i = 0; i < lightCount; i++)
                {
                    zone.Lights.Add(new EqgLight
                    {
                        Name = GetString(strings, reader.ReadInt32()),
                        Position = ReadVector3(reader),
                        Color = ReadVector3(reader),
                        Radius = reader.ReadSingle()
                    });
                }

                return zone;
            }
            catch (Exception e) when (e is EndOfStreamException || e is ArgumentException)
            {
                logger.LogError($"EqgReader: Failed to parse zone {fileName}: {e.Message}");
                return null;
            }
        }

        private static EqgModel ReadModOrTer(byte[] bytes, string fileName, EqgModelType type)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));
            reader.ReadUInt32();
            var version = (int)reader.ReadUInt32();
            var stringTableSize = reader.ReadUInt32();
            var materialCount = reader.ReadUInt32();
            var vertexCount = reader.ReadUInt32();
            var triangleCount = reader.ReadUInt32();
            var boneCount = type == EqgModelType.Mod ? reader.ReadUInt32() : 0;
            var strings = reader.ReadBytes((int)stringTableSize);

            var model = CreateModel(fileName, type, version);
            ReadMaterials(reader, strings, materialCount, model);

            var piece = new EqgMeshPiece { Name = model.Name, HasVertexColors = version >= 3 };
            ReadVertices(reader, vertexCount, version, piece);
            ReadTriangles(reader, triangleCount, piece);
            model.Pieces.Add(piece);

            if (boneCount > 0)
            {
                ReadBones(reader, strings, boneCount, model);
                piece.Weights = ReadWeights(reader, vertexCount);
            }

            model.ResolveBoneParents();
            return model;
        }

        private static EqgModel ReadMds(byte[] bytes, string fileName)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));
            reader.ReadUInt32();
            var version = (int)reader.ReadUInt32();
            var stringTableSize = reader.ReadUInt32();
            var materialCount = reader.ReadUInt32();
            var boneCount = reader.ReadUInt32();
            var pieceCount = reader.ReadUInt32();
            var strings = reader.ReadBytes((int)stringTableSize);

            var model = CreateModel(fileName, EqgModelType.Mds, version);
            ReadMaterials(reader, strings, materialCount, model);
            ReadBones(reader, strings, boneCount, model);

            for (var i = 0; i < pieceCount; i++)
            {
                reader.ReadUInt32(); // main piece flag
                var piece = new EqgMeshPiece
                {
                    Name = GetString(strings, reader.ReadInt32()),
                    HasVertexColors = version >= 3
                };
                var vertexCount = reader.ReadUInt32();
                var triangleCount = reader.ReadUInt32();
                var weightCount = reader.ReadUInt32();

                ReadVertices(reader, vertexCount, version, piece);
                ReadTriangles(reader, triangleCount, piece);

                if (weightCount > 0)
                {
                    piece.Weights = ReadWeights(reader, weightCount);
                }

                model.Pieces.Add(piece);
            }

            model.ResolveBoneParents();
            return model;
        }

        private static EqgModel CreateModel(string fileName, EqgModelType type, int version)
        {
            return new EqgModel
            {
                Name = Path.GetFileNameWithoutExtension(fileName),
                FileName = fileName,
                Type = type,
                Version = version
            };
        }

        private static void ReadMaterials(BinaryReader reader, byte[] strings, uint count, EqgModel model)
        {
            for (var i = 0; i < count; i++)
            {
                var material = new EqgMaterial
                {
                    Index = reader.ReadInt32(),
                    Name = GetString(strings, reader.ReadInt32()),
                    Shader = GetString(strings, reader.ReadInt32())
                };

                var propertyCount = reader.ReadUInt32();
                for (var j = 0; j < propertyCount; j++)
                {
                    var property = new EqgMaterialProperty
                    {
                        Name = GetString(strings, reader.ReadInt32()),
                        Type = (EqgMaterialPropertyType)reader.ReadUInt32()
                    };

                    switch (property.Type)
                    {
                        case EqgMaterialPropertyType.Float:
                            property.FloatValue = reader.ReadSingle();
                            break;
                        case EqgMaterialPropertyType.Texture:
                            property.StringValue = GetString(strings, reader.ReadInt32());
                            break;
                        default:
                            property.IntValue = reader.ReadUInt32();
                            break;
                    }

                    material.Properties.Add(property);
                }

                model.Materials.Add(material);
            }
        }

        private static void ReadVertices(BinaryReader reader, uint count, int version, EqgMeshPiece piece)
        {
            for (var i = 0; i < count; i++)
            {
                var vertex = new EqgVertex
                {
                    Position = ReadVector3(reader),
                    Normal = ReadVector3(reader),
                    Color = Vector4.One
                };

                if (version >= 3)
                {
                    // D3DCOLOR, stored as BGRA bytes
                    var b = reader.ReadByte();
                    var g = reader.ReadByte();
                    var r = reader.ReadByte();
                    var a = reader.ReadByte();
                    vertex.Color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
                    vertex.Uv = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    reader.ReadSingle(); // secondary UV set
                    reader.ReadSingle();
                }
                else
                {
                    vertex.Uv = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }

                piece.Vertices.Add(vertex);
            }
        }

        private static void ReadTriangles(BinaryReader reader, uint count, EqgMeshPiece piece)
        {
            for (var i = 0; i < count; i++)
            {
                piece.Triangles.Add(new EqgTriangle
                {
                    V0 = reader.ReadInt32(),
                    V1 = reader.ReadInt32(),
                    V2 = reader.ReadInt32(),
                    MaterialIndex = reader.ReadInt32(),
                    Flags = reader.ReadUInt32()
                });
            }
        }

        private static void ReadBones(BinaryReader reader, byte[] strings, uint count, EqgModel model)
        {
            for (var i = 0; i < count; i++)
            {
                var bone = new EqgBone
                {
                    Name = GetString(strings, reader.ReadInt32()),
                    NextSibling = reader.ReadInt32(),
                    ChildCount = reader.ReadInt32(),
                    FirstChild = reader.ReadInt32()
                };

                var translation = ReadVector3(reader);
                var rotation = ReadQuaternion(reader);
                var scale = ReadVector3(reader);
                SanitizeTransform(ref translation, ref rotation, ref scale);
                bone.Translation = translation;
                bone.Rotation = rotation;
                bone.Scale = scale;
                model.Bones.Add(bone);
            }
        }

        private static EqgVertexWeights[] ReadWeights(BinaryReader reader, uint count)
        {
            var weights = new EqgVertexWeights[count];
            for (var i = 0; i < count; i++)
            {
                weights[i] = new EqgVertexWeights
                {
                    Count = reader.ReadInt32(),
                    Bone0 = reader.ReadInt32(), Weight0 = reader.ReadSingle(),
                    Bone1 = reader.ReadInt32(), Weight1 = reader.ReadSingle(),
                    Bone2 = reader.ReadInt32(), Weight2 = reader.ReadSingle(),
                    Bone3 = reader.ReadInt32(), Weight3 = reader.ReadSingle()
                };
            }

            return weights;
        }

        /// <summary>
        /// Some client files contain NaN or zero scale transforms (usually on unused attachment points).
        /// Replace them with identity values so they produce valid output.
        /// </summary>
        private static void SanitizeTransform(ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
        {
            if (!IsFinite(translation.X) || !IsFinite(translation.Y) || !IsFinite(translation.Z))
            {
                translation = Vector3.Zero;
            }

            if (!IsFinite(rotation.X) || !IsFinite(rotation.Y) || !IsFinite(rotation.Z) || !IsFinite(rotation.W) ||
                rotation.LengthSquared() < 1e-8f)
            {
                rotation = Quaternion.Identity;
            }

            if (!IsFinite(scale.X) || !IsFinite(scale.Y) || !IsFinite(scale.Z) ||
                Math.Abs(scale.X) < 1e-6f || Math.Abs(scale.Y) < 1e-6f || Math.Abs(scale.Z) < 1e-6f)
            {
                scale = Vector3.One;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static Vector3 ReadVector3(BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        /// <summary>
        /// Bone and keyframe rotations are stored as the conjugate (inverse) of the rotation they apply
        /// </summary>
        private static Quaternion ReadQuaternion(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();
            return new Quaternion(-x, -y, -z, w);
        }

        private static string GetString(byte[] strings, int offset)
        {
            if (offset < 0 || offset >= strings.Length)
            {
                return string.Empty;
            }

            var end = Array.IndexOf(strings, (byte)0, offset);
            if (end < 0)
            {
                end = strings.Length;
            }

            return Encoding.ASCII.GetString(strings, offset, end - offset);
        }
    }
}
