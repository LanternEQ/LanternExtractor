using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x36 - Mesh
    /// Contains geometric data making up a mesh
    /// </summary>
    public class Mesh : WldFragment
    {
        /// <summary>
        /// The center of the mesh - used to calculate absolute coordinates of vertices
        /// I used to store absolute coordinates but animated meshes use coordinates relative to the center so we have to keep it separate
        /// </summary>
        public vec3 Center { get; private set; }

        /// <summary>
        /// The maximum distance between the center and any vertex - bounding radius
        /// </summary>
        public float MaxDistance { get; private set; }

        /// <summary>
        /// The minimum vertex positions in the model - used for bounding box
        /// </summary>
        public vec3 MinPosition { get; private set; }

        /// <summary>
        /// The maximum vertex positions in the model - used for bounding box
        /// </summary>
        public vec3 MaxPosition { get; private set; }

        /// <summary>
        /// The texture list used to render this mesh
        /// In zone meshes, it's always the same one
        /// In object meshes, it can be unique
        /// </summary>
        public MaterialList MaterialList { get; private set; }

        /// <summary>
        /// The vertices of the mesh
        /// </summary>
        public List<vec3> Vertices { get; private set; }
        
        /// <summary>
        /// The normals of the mesh
        /// </summary>
        public List<vec3> Normals { get; private set; }

        /// <summary>
        /// The polygon indices of the mesh
        /// </summary>
        public List<Polygon> Indices { get; private set; }
        
        public List<Color> Colors { get; private set; }

        /// <summary>
        /// The UV texture coordinates of the vertex
        /// </summary>
        public List<vec2> TextureUvCoordinates { get; private set; }

        /// <summary>
        /// The mesh render groups
        /// Defines which texture index corresponds with groups of vertices
        /// </summary>
        public List<RenderGroup> MaterialGroups { get; private set; }

        /// <summary>
        /// The animated vertex fragment (0x37) reference
        /// </summary>
        public MeshAnimatedVertices AnimatedVertices { get; private set; }

        /// <summary>
        /// Set to true if there are non solid polygons in the mesh
        /// This means we export collision separately (e.g. trees, fire)
        /// </summary>
        public bool ExportSeparateCollision { get; private set; }

        public bool Handled = false;

        public int StartTextureIndex = 0;

        /// <summary>
        /// The render components of a mob skeleton
        /// </summary>
        public Dictionary<int, MobVertexPiece> MobPieces { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            if (flags == 0x00018003)
            {
                // zone mesh
            }
            else if (flags == 0x00014003)
            {
                // placeable object
            }

            if (Name.ToLower().Contains("templife"))
            {
                
            }

            int textureList = reader.ReadInt32();

            MaterialList = fragments[textureList - 1] as MaterialList;

            int meshAnimation = reader.ReadInt32();

            if (meshAnimation != 0)
            {
                AnimatedVertices = fragments[meshAnimation - 2] as MeshAnimatedVertices;
            }

            int unknown = reader.ReadInt32();

            // maybe references the first 0x03 in the WLD - unknown
            int unknown2 = reader.ReadInt32();

            Center = new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            // 3 unknown dwords
            reader.BaseStream.Position += (4 * 3);

            MaxDistance = reader.ReadSingle();

            MinPosition = new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            MaxPosition = new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            Vertices = new List<vec3>();
            Normals = new List<vec3>();
            Colors = new List<Color>();
            TextureUvCoordinates = new List<vec2>();

            short vertexCount = reader.ReadInt16();

            short textureCoordinateCount = reader.ReadInt16();

            short normalsCount = reader.ReadInt16();

            short colorsCount = reader.ReadInt16();

            short polygonCount = reader.ReadInt16();

            short vertexPieceCount = reader.ReadInt16();

            short polygonTextureCount = reader.ReadInt16();

            short vertexTextureCount = reader.ReadInt16();

            short size9 = reader.ReadInt16();

            float scale = 1.0f / (1 << reader.ReadInt16());
            
            for (int i = 0; i < vertexCount; ++i)
            {
                Vertices.Add(new vec3(reader.ReadInt16() * scale, reader.ReadInt16() * scale,
                    reader.ReadInt16() * scale));
            }

            for (int i = 0; i < textureCoordinateCount; ++i)
            {
                if (isNewWldFormat)
                {
                    TextureUvCoordinates.Add(new vec2(reader.ReadInt32() / 256.0f, reader.ReadInt32() / 256.0f));
                }
                else
                {
                    TextureUvCoordinates.Add(new vec2(reader.ReadInt16() / 256.0f, reader.ReadInt16() / 256.0f));
                }
            }

            for (int i = 0; i < normalsCount; ++i)
            {
                float x = reader.ReadSByte() / 127.0f;
                float y = reader.ReadSByte() / 127.0f;
                float z = reader.ReadSByte() / 127.0f;
                
                Normals.Add(new vec3(x, y, z));
            }

            for (int i = 0; i < colorsCount; ++i)
            {
                int color = reader.ReadInt32();
                
                byte[] colorBytes = BitConverter.GetBytes(color);
                int b = colorBytes[0];
                int g = colorBytes[1];
                int r = colorBytes[2];
                int a = colorBytes[3];
                
                Colors.Add(new Color{R = r, G = g, B = b, A = a});
            }

            Indices = new List<Polygon>();

            for (int i = 0; i < polygonCount; ++i)
            {
                bool isSolid = (reader.ReadInt16() == 0);

                if (!isSolid)
                {
                    ExportSeparateCollision = true;
                }

                Indices.Add(new Polygon()
                {
                    Solid = isSolid,
                    Vertex1 = reader.ReadInt16(),
                    Vertex2 = reader.ReadInt16(),
                    Vertex3 = reader.ReadInt16(),
                });
            }
            
            MobPieces = new Dictionary<int, MobVertexPiece>();
            int mobStart = 0;

            for (int i = 0; i < vertexPieceCount; ++i)
            {
                int count = reader.ReadInt16();
                int index1 = reader.ReadInt16();
                var mobVertexPiece = new MobVertexPiece
                {
                    Count = count,
                    Start = mobStart
                };

                mobStart += count;

                MobPieces[index1] = mobVertexPiece;
            }

            MaterialGroups = new List<RenderGroup>();

            StartTextureIndex = Int32.MaxValue;
            
            for (int i = 0; i < polygonTextureCount; ++i)
            {
                var group = new RenderGroup();
                group.PolygonCount = reader.ReadUInt16();
                group.MaterialIndex = reader.ReadUInt16();
                MaterialGroups.Add(group);

                if (group.MaterialIndex < StartTextureIndex)
                {
                    StartTextureIndex = group.MaterialIndex;
                }
            }

            for (int i = 0; i < vertexTextureCount; ++i)
            {
                reader.BaseStream.Position += 4;
            }

            for (int i = 0; i < size9; ++i)
            {
                reader.BaseStream.Position += 12;
            }
            
            // In some rare cases, the number of uvs does not match the number of vertices
            if (Vertices.Count != TextureUvCoordinates.Count)
            {
                int difference = Vertices.Count - TextureUvCoordinates.Count;

                for (int i = 0; i < difference; ++i)
                {
                    TextureUvCoordinates.Add(new vec2(0.0f, 0.0f));
                }
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x36: Center: " + Center);
            logger.LogInfo("0x36: Max distance: " + MaxDistance);
            logger.LogInfo("0x36: Min position: " + MinPosition);
            logger.LogInfo("0x36: Max position: " + MaxDistance);
            logger.LogInfo("0x36: Texture list reference: " + MaterialList.Index);
            logger.LogInfo("0x36: Vertex count: " + Vertices.Count);
            logger.LogInfo("0x36: Polygon count: " + Indices.Count);
            logger.LogInfo("0x36: Texture coordinate count: " + TextureUvCoordinates.Count);
            logger.LogInfo("0x36: Render group count: " + MaterialGroups.Count);
            logger.LogInfo("0x36: Export separate collision: " + ExportSeparateCollision);

            if (AnimatedVertices != null)
            {
                logger.LogInfo("0x36: Animated mesh vertices reference: " + AnimatedVertices.Index);
            }
        }

        public void ShiftVertices(vec3 shift, int index)
        {
            if (!MobPieces.ContainsKey(index))
            {
                return;
            }

            MobVertexPiece mobPiece = MobPieces[index];

            for (int i = 0; i < mobPiece.Count; ++i)
            {
                Vertices[mobPiece.Start + i] = Vertices[mobPiece.Start + i] + shift;
            }
        }

        private void ShiftAndRotateVertices(vec3 shift, quat rotation, int index, ILogger logger)
        {
            if (!MobPieces.ContainsKey(index))
            {
                return;
            }

            MobVertexPiece mobPiece = MobPieces[index];

            for (int i = 0; i < mobPiece.Count; ++i)
            {
                vec3 vertexPosition = Vertices[mobPiece.Start + i];
                vertexPosition += shift;
                vertexPosition *= rotation;
                Vertices[mobPiece.Start + i] = vertexPosition;
            }
        }
        
        private void ShiftAndRotateVertices(mat4 rotationTranslationMat, int index, ILogger logger)
        {
            if (!MobPieces.ContainsKey(index))
            {
                return;
            }

            var quat = glm.ToQuaternion(rotationTranslationMat);

            //logger.LogError( $"{index} trans: " + rotationTranslationMat.Column3.ToString());
            logger.LogError( $"{index} rot: " + quat.ToString());

            MobVertexPiece mobPiece = MobPieces[index];

            for (int i = 0; i < mobPiece.Count; ++i)
            {
                var position = Vertices[mobPiece.Start + i];
                vec4 newPosition = new vec4(position, 1.0f);
                vec4 shiftedPosition = rotationTranslationMat * newPosition;
                Vertices[mobPiece.Start + i] = shiftedPosition.xyz;

                if (position != shiftedPosition.xyz)
                {
                    
                }
            }
        }

        private static void AddIfNotContained(List<int> list, int element)
        {
            if (list.Contains(element))
            {
                return;
            }

            list.Add(element);
        }

        public static string FixCharacterMeshName(string meshName2, bool isMainModel)
        {
            if (!isMainModel)
            {
                Regex expression = new Regex("^(\\w{3})(.*)(\\d{2})_DMSPRITEDEF$");
            
                if (!expression.IsMatch(meshName2))
                {
                    Console.WriteLine(
                        "Error dealing with mesh: " + meshName2);
                    Console.WriteLine("Mesh is not valid: "  + meshName2);
                    return string.Empty;
                }

                var match = expression.Match(meshName2);
                string actorName = match.Groups[1].ToString();
                string meshName = match.Groups[2].ToString();
                string skinId = match.Groups[3].ToString();

                int partNumber = Convert.ToInt32(skinId);

                return (actorName + meshName + partNumber).ToLower();
            }
            
            string fixedMeshName = meshName2;

            if (!fixedMeshName.Contains("_DMSPRITEDEF"))
            {
                return fixedMeshName.ToLower();
            }

            var nameParts = fixedMeshName.Split('_');

            if (nameParts.Length == 2)
            {
                fixedMeshName = nameParts[0];
            }

            return fixedMeshName.ToLower();
        }
        
        public string ParseMeshNameDetails()
        {
            Regex expression = new Regex("^(\\w{3})(.*)(\\d{2})_DMSPRITEDEF$");
            
            if (!expression.IsMatch(Name))
            {
                Console.WriteLine(
                    "Error dealing with mesh: " + Name);
                Console.WriteLine("Mesh is not valid: "  + Name);
                return string.Empty;
            }

            var match = expression.Match(Name);
            string actorName = match.Groups[1].ToString();
            string meshName = match.Groups[2].ToString();
            string skinId = match.Groups[3].ToString();

            int partNumber = Convert.ToInt32(skinId);

            if (partNumber == 0)
            {
                return actorName;
            }
            
            return actorName + partNumber;
        }
        
        /// <summary>
        /// Recursively shifts the position and rotation of a skeleton to get each frame model
        /// Will be expanded soon to support rotations so we can fully export characted model animations.
        /// </summary>
        /// <param name="mesh">The mesh to shift</param>
        /// <param name="track">The skeleton track set to shift</param>
        /// <param name="skeletonPieceData">The data about this specific piece</param>
        /// <param name="parentShift">The shift value to apply to this piece</param>
        /// <param name="index">The current bone index</param>
        public void ShiftSkeletonValues(SkeletonHierarchy track, SkeletonPieceData skeletonPieceData,
            vec3 parentShift, int index)
        {
            vec3 newShift = parentShift + skeletonPieceData.AnimationTracks.First().Value.TrackDefFragment.Frames[0].Translation;

            ShiftVertices(newShift, index);

            foreach (var pieces in skeletonPieceData.ConnectedPieces)
            {
                ShiftSkeletonValues(track, track.Skeleton[pieces], newShift, pieces);
            }
        }
        
        public void ShiftSkeletonValues(List<SkeletonNode> skeleton, SkeletonNode currentNode,
            vec3 parentShift, int index)
        {
            vec3 newShift = parentShift + currentNode.Track.TrackDefFragment.Frames2[0].Translation;

            ShiftVertices(newShift, index);

            foreach (var pieces in skeleton[index].Children)
            {
                ShiftSkeletonValues(skeleton, skeleton[pieces], newShift, pieces);
            }
        }
        
        public void ShiftSkeletonValues(List<SkeletonNode> skeleton, List<BoneTrack> boneTracks,
            mat4 parentModelMatrix, int index, int frame, ILogger logger)
        {
            int actualFrame = frame;
            
            if (boneTracks[index]._frames.Count == 1)
            {
                actualFrame = 0;
            }

            mat4 translationMatrix = mat4.Translate(boneTracks[index]._frames[actualFrame].Translation);
            mat4 rotationMatrix = glm.ToMat4(boneTracks[index]._frames[actualFrame].Rotation);
            mat4 modelMatrix = translationMatrix * rotationMatrix;
            mat4 combineParentMatrix = parentModelMatrix * modelMatrix;
            
            ShiftAndRotateVertices(combineParentMatrix, index, logger);
            
            foreach (var pieces in skeleton[index].Children)
            {
                ShiftSkeletonValues(skeleton, boneTracks, combineParentMatrix, pieces, frame, logger);
            }
        }
        
        public void ShiftSkeletonValues(List<SkeletonNode> skeleton, List<BoneTrack> boneTracks,
            BoneTransform parentTrans, int index, int frame)
        {
            int maxFrame = Math.Min(boneTracks[index]._frames.Count - 1, frame);
            
            BoneTransform pieceTrans = boneTracks[index]._frames[maxFrame];
            BoneTransform globalTrans = parentTrans.Map(pieceTrans);
            
            ShiftVertices(globalTrans.Translation, index);

            foreach (var pieces in skeleton[index].Children)
            {
                ShiftSkeletonValues(skeleton, boneTracks, globalTrans, pieces, frame);
            }
        }

        public string GetIntermediateMeshExport(int skinId, Dictionary<string,Dictionary<int, Material>> materials)
        {
            var export = new StringBuilder();

            export.AppendLine("# Lantern Test Intermediate Format");

            foreach (var vertex in Vertices)
            {
                export.Append("v");
                export.Append(",");
                export.Append(vertex.x);
                export.Append(",");
                export.Append(vertex.y);
                export.Append(",");
                export.Append(vertex.z);
                export.AppendLine();
            }
            
            foreach (var textureUv in TextureUvCoordinates)
            {
                export.Append("t");
                export.Append(",");
                export.Append(textureUv.x);
                export.Append(",");
                export.Append(textureUv.y);
                export.AppendLine();
            }
            
            foreach (var normal in Normals)
            {
                export.Append("n");
                export.Append(",");
                export.Append(normal.x);
                export.Append(",");
                export.Append(normal.y);
                export.Append(",");
                export.Append(normal.z);
                export.AppendLine();
            }
            
            int currentPolygon = 0;
            foreach (RenderGroup group in MaterialGroups)
            {
                string materialName = string.Empty;

                if (skinId == -1)
                {
                    //materialName = MaterialList.GetMaterialPrefix(MaterialList.Materials[group.TextureIndex].ShaderType) + MaterialList.Materials[group.TextureIndex].GetFirstBitmapNameWithoutExtension();
                    materialName = MaterialList.GetMaterialPrefix(MaterialList.Materials[group.MaterialIndex].ShaderType) + MaterialList.Materials[group.MaterialIndex].GetMaterialSkinWithoutExtension();
                }
                else
                {
                    //materialName = MaterialList.GetMaterialPrefix(MaterialList.Materials[group.TextureIndex].ShaderType) + MaterialList.Materials[group.TextureIndex].GetSpecificMaterialSkinWithoutExtension(skinId);
                    //materialName = MaterialList.GetMaterialPrefix(MaterialList.Materials[group.TextureIndex].ShaderType) + MaterialList.Materials[group.TextureIndex].GetMaterialSkinWithoutExtension();
                    //materialName = MaterialList.GetMaterialPrefix(MaterialList.Materials[group.TextureIndex].ShaderType) + MaterialList.Materials[group.TextureIndex].GetFirstBitmapNameWithoutExtension();

                    string nameOfMaterial = MaterialList.Materials[group.MaterialIndex].GetFirstBitmapNameWithoutExtension().ToUpper() + "_MDF";
                    
                    if (nameOfMaterial.ToLower().Contains("fun"))
                    {
                        
                    }
                    
                    if (nameOfMaterial == "DRAHE0612_MDF")
                    {
                    
                    }
                    
                    string charName;
                    int skinIdUnused;
                    string partName;
                    
                    if (!WldMaterialPalette.ExplodeName(nameOfMaterial, out charName, out skinIdUnused, out partName))
                    {
                        materialName =
                            MaterialList.GetMaterialPrefix(MaterialList.Materials[group.MaterialIndex].ShaderType) +
                            MaterialList.Materials[group.MaterialIndex].GetFirstBitmapNameWithoutExtension();                    }
                    else
                    {
                        if (materials.ContainsKey(partName))
                        {
                        
                            var specific = materials[partName];

                            if (specific.ContainsKey(skinId))
                            {
                                materialName =
                                    MaterialList.GetMaterialPrefix(MaterialList.Materials[group.MaterialIndex].ShaderType) +
                                    specific[skinId].GetFirstBitmapNameWithoutExtension();

                            }
                            else
                            {
                                // Find the lowest value
                                int lowest = Int32.MaxValue;

                                foreach (var entry in specific)
                                {
                                    if (entry.Key < lowest)
                                    {
                                        lowest = entry.Key;
                                    }
                                }
                                
                                materialName =
                                    MaterialList.GetMaterialPrefix(MaterialList.Materials[group.MaterialIndex].ShaderType) +
                                    specific[lowest].GetFirstBitmapNameWithoutExtension();
                            }
                        }
                        else
                        {
                            materialName = MaterialList.GetMaterialPrefix(MaterialList.Materials[group.MaterialIndex].ShaderType) +
                                           nameOfMaterial;
                            // what the fuck?
                        }
                    }
                }

                export.Append("mg");
                export.Append(",");
                export.Append(group.MaterialIndex - StartTextureIndex);
                export.Append(",");
                export.Append(materialName);
                export.AppendLine();
                
                for (int i = 0; i < group.PolygonCount; ++i)
                {
                    int vertex1 = Indices[currentPolygon].Vertex1;
                    int vertex2 = Indices[currentPolygon].Vertex2;
                    int vertex3 = Indices[currentPolygon].Vertex3;

                    export.Append("i");
                    export.Append(",");
                    export.Append(group.MaterialIndex - StartTextureIndex);
                    export.Append(",");
                    export.Append(vertex3);
                    export.Append(",");
                    export.Append(vertex2);
                    export.Append(",");
                    export.Append(vertex1);
                    export.AppendLine();
                    currentPolygon++;
                }
            }
            
            foreach (var bone in MobPieces)
            {
                export.Append("b");
                export.Append(",");
                export.Append(bone.Key);
                export.Append(",");
                export.Append(bone.Value.Start);
                export.Append(",");
                export.Append(bone.Value.Count);
                export.AppendLine();
            }

            if (StartTextureIndex != 0)
            {
                
            }
            
            return export.ToString();        
        }
    }
}