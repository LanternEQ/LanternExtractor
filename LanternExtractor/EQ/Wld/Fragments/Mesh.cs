using System;
using System.Collections.Generic;
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
    class Mesh : WldFragment
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
        /// The polygon indices of the mesh
        /// </summary>
        public List<Polygon> Polygons { get; private set; }

        /// <summary>
        /// The UV texture coordinates of the vertex
        /// </summary>
        public List<vec2> TextureUvCoordinates { get; private set; }

        /// <summary>
        /// The mesh render groups
        /// Defines which texture index corresponds with groups of vertices
        /// </summary>
        public List<RenderGroup> RenderGroups { get; private set; }

        /// <summary>
        /// The animated vertex fragment (0x37) reference
        /// </summary>
        public MeshAnimatedVertices AnimatedVertices { get; private set; }

        /// <summary>
        /// Set to true if there are non solid polygons in the mesh
        /// This means we export collision separately (e.g. trees, fire)
        /// </summary>
        public bool ExportSeparateCollision { get; private set; }

        /// <summary>
        /// The render components of a mob skeleton
        /// </summary>
        public Dictionary<int, MobVertexPiece> MobPieces { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
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
                reader.BaseStream.Position += 3;
            }

            for (int i = 0; i < colorsCount; ++i)
            {
                reader.BaseStream.Position += 4;
            }

            Polygons = new List<Polygon>();

            for (int i = 0; i < polygonCount; ++i)
            {
                bool isSolid = (reader.ReadInt16() == 0);

                if (!isSolid)
                {
                    ExportSeparateCollision = true;
                }

                Polygons.Add(new Polygon()
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
            
            RenderGroups = new List<RenderGroup>();

            for (int i = 0; i < polygonTextureCount; ++i)
            {
                var group = new RenderGroup();
                group.PolygonCount = reader.ReadUInt16();
                group.TextureIndex = reader.ReadUInt16();
                RenderGroups.Add(group);
            }

            for (int i = 0; i < vertexTextureCount; ++i)
            {
                reader.BaseStream.Position += 4;
            }

            for (int i = 0; i < size9; ++i)
            {
                reader.BaseStream.Position += 12;
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
            logger.LogInfo("0x36: Polygon count: " + Polygons.Count);
            logger.LogInfo("0x36: Texture coordinate count: " + TextureUvCoordinates.Count);
            logger.LogInfo("0x36: Render group count: " + RenderGroups.Count);
            logger.LogInfo("0x36: Export separate collision: " + ExportSeparateCollision);

            if (AnimatedVertices != null)
            {
                logger.LogInfo("0x36: Animated mesh vertices reference: " + AnimatedVertices.Index);
            }
        }

        /// <summary>
        /// Exports the model to an .obj file
        /// </summary>
        /// <param name="baseVertex">The number of vertices we have processed so far in the model</param>
        /// <param name="objExportType"></param>
        /// <param name="vertexCount">The number of vertices in this model</param>
        /// <param name="activeMaterial"></param>
        /// <param name="lastUsedTexture"></param>
        /// <returns></returns>
        public List<string> GetMeshExport(int baseVertex, Material activeMaterial, ObjExportType objExportType,
            out int vertexCount, out Material lastUsedMaterial, Settings settings, ILogger logger)
        {
            var frames = new List<string>();
            var usedVertices = new List<int>();
            var unusedVertices = new List<int>();

            int currentPolygon = 0;

            var faceOutput = new StringBuilder();

            // First assemble the faces that are needed
            foreach (RenderGroup group in RenderGroups)
            {
                int textureIndex = group.TextureIndex;
                int polygonCount = group.PolygonCount;

                List<int> activeArray = null;
                bool bitmapValid = false;

                if (objExportType == ObjExportType.Textured)
                {
                    if(MaterialList.Materials[textureIndex].ShaderType != ShaderType.Invisible)
                    {
                        activeArray = usedVertices;
                    }
                    else
                    {
                        if(settings.ExportHiddenGeometry)
                        {
                            activeArray = usedVertices;
                        }
                        else
                        {
                            activeArray = unusedVertices;
                        }
                    }

                    bitmapValid = true;
                }
                else if (objExportType == ObjExportType.Collision)
                {
                    activeArray = usedVertices;
                    bitmapValid = false;
                }

                if (textureIndex < 0 || textureIndex >= MaterialList.Materials.Count)
                {
                    logger.LogError("Invalid texture index");
                    continue;
                }

                string filenameWithoutExtension = MaterialList.Materials[textureIndex].GetFirstBitmapNameWithoutExtension();

                if(MaterialList.Materials[textureIndex].ShaderType != ShaderType.Invisible
                    || (MaterialList.Materials[textureIndex].ShaderType == ShaderType.Invisible && settings.ExportHiddenGeometry))
                {
                    // Material change
                    if (activeMaterial != MaterialList.Materials[textureIndex])
                    {
                        if (string.IsNullOrEmpty(filenameWithoutExtension))
                        {
                            faceOutput.AppendLine(LanternStrings.ObjUseMtlPrefix + "null");
                        }
                        else
                        {
                            string materialPrefix =
                                    MaterialList.GetMaterialPrefix(MaterialList.Materials[textureIndex].ShaderType);
                            faceOutput.AppendLine(LanternStrings.ObjUseMtlPrefix + materialPrefix + filenameWithoutExtension);
                            activeMaterial = MaterialList.Materials[textureIndex];
                        }
                    }
                }

                for (int j = 0; j < polygonCount; ++j)
                {
                    if(currentPolygon < 0 || currentPolygon >= Polygons.Count)
                    {
                        logger.LogError("Invalid polygon index");
                        continue;
                    }

                    int vertex1 = Polygons[currentPolygon].Vertex1 + baseVertex + 1;
                    int vertex2 = Polygons[currentPolygon].Vertex2 + baseVertex + 1;
                    int vertex3 = Polygons[currentPolygon].Vertex3 + baseVertex + 1;

                    if (!Polygons[currentPolygon].Solid && objExportType == ObjExportType.Collision)
                    {
                        continue;
                    }
                    
                    if (activeArray == usedVertices)
                    {
                        int index1 = vertex1 - unusedVertices.Count;
                        int index2 = vertex2 - unusedVertices.Count;
                        int index3 = vertex3 - unusedVertices.Count;

                        // Vertex + UV
                        if (objExportType != ObjExportType.Collision)
                        {
                            faceOutput.AppendLine("f " + index3 + "/" + index3 + " "
                                                  + index2 + "/" + index2 + " " +
                                                  +index1 + "/" + index1);
                        }
                        else
                        {
                            faceOutput.AppendLine("f " + index3 + " "
                                                  + index2 + " " +
                                                  +index1);
                        }
                    }

                    AddIfNotContained(activeArray, Polygons[currentPolygon].Vertex1);
                    AddIfNotContained(activeArray, Polygons[currentPolygon].Vertex2);
                    AddIfNotContained(activeArray, Polygons[currentPolygon].Vertex3);

                    currentPolygon++;
                }
            }

            var vertexOutput = new StringBuilder();

            usedVertices.Sort();

            int frameCount = 1;

            if (AnimatedVertices != null)
            {
                frameCount += AnimatedVertices.Frames.Count;
            }

            for (int i = 0; i < frameCount; ++i)
            {
                // Add each vertex
                foreach (var usedVertex in usedVertices)
                {
                    vec3 vertex;

                    if (i == 0)
                    {
                        if(usedVertex < 0 || usedVertex >= Vertices.Count)
                        {
                            logger.LogError("Invalid vertex index: " + usedVertex);
                            continue;
                        }

                        vertex = Vertices[usedVertex];
                    }
                    else
                    {
                        if (AnimatedVertices == null)
                        {
                            continue;
                        }

                        vertex = AnimatedVertices.Frames[i - 1][usedVertex];
                    }

                    vertexOutput.AppendLine("v " + -(vertex.x + Center.x) + " " + (vertex.z + Center.z) + " " +
                                            (vertex.y + Center.y));

                    if (objExportType == ObjExportType.Collision)
                    {
                        continue;
                    }

                    if(usedVertex >= TextureUvCoordinates.Count)
                    {
                        vertexOutput.AppendLine("vt " + 0.0f + " " + 0.0f);

                        continue;
                    }

                    vec2 vertexUvs = TextureUvCoordinates[usedVertex];
                    vertexOutput.AppendLine("vt " + vertexUvs.x + " " + vertexUvs.y);
                }

                frames.Add(vertexOutput.ToString() + faceOutput);
                vertexOutput.Clear();
            }


            vertexCount = usedVertices.Count;
            lastUsedMaterial = activeMaterial;

            // Ensure that output use the decimal point rather than the comma (as in Germany)
            for (var i = 0; i < frames.Count; i++)
            {
                frames[i] = frames[i].Replace(',', '.');
            }

            return frames;
        }

        /// <summary>
        /// Returns a string containing an OBJ export for this mesh
        /// </summary>
        /// <param name="forceMaterialList">Forces the export to use a specific material list - e.g. model head</param>
        /// <returns>The OBJ export</returns>
        public string GetSkeletonMeshExport(string forceMaterialList = null)
        {
            var export = new StringBuilder();

            export.AppendLine(LanternStrings.ObjMaterialHeader + (string.IsNullOrEmpty(forceMaterialList)
                                  ? FixCharacterMeshName(Name, true)
                                  : FixCharacterMeshName(forceMaterialList, true)) + LanternStrings.FormatMtlExtension);

            for (var i = 0; i < Vertices.Count; i++)
            {
                vec3 vertex = Vertices[i];
                vec2 uvs = TextureUvCoordinates[i];
                export.AppendLine("v " + (vertex.x + Center.x) + " " + (vertex.y + Center.y) + " " +
                                  (vertex.z + Center.z));
                export.AppendLine("vt " + uvs.x + " " + uvs.y);
            }

            int currentPolygon = 0;
            foreach (RenderGroup group in RenderGroups)
            {              
                string bitmapName = MaterialList.Materials[group.TextureIndex].TextureInfoReference.TextureInfo
                    .BitmapNames[0].Filename;

                string slotName = string.Empty;

                slotName = MaterialList.Materials[group.TextureIndex].ExportName;
                

                string pngName = bitmapName.Substring(0, bitmapName.Length - 4);
                
                string materialPrefix =
                    MaterialList.GetMaterialPrefix(MaterialList.Materials[group.TextureIndex].ShaderType);
                export.AppendLine(LanternStrings.ObjUseMtlPrefix + materialPrefix + slotName);

                for (int i = 0; i < group.PolygonCount; ++i)
                {
                    int vertex1 = Polygons[currentPolygon].Vertex1 + 1;
                    int vertex2 = Polygons[currentPolygon].Vertex2 + 1;
                    int vertex3 = Polygons[currentPolygon].Vertex3 + 1;

                    export.AppendLine("f " + vertex3 + "/" + vertex3 + " "
                                      + vertex2 + "/" + vertex2 + " " +
                                      +vertex1 + "/" + vertex1);

                    currentPolygon++;
                }
            }

            // Ensure that output use the decimal point rather than the comma (as in Germany)
            string exportString = export.ToString().Replace(',', '.');
            
            return exportString;
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
        /// <param name="shift">The shift value to apply to this piece</param>
        /// <param name="index">The current bone index</param>
        public void ShiftSkeletonValues(SkeletonTrack track, SkeletonPieceData skeletonPieceData,
            vec3 shift, int index)
        {
            vec3 newShift = shift + skeletonPieceData.AnimationTracks.First().Value.SkeletonPiece.Frames[0].Translation;

            ShiftVertices(newShift, index);

            foreach (var pieces in skeletonPieceData.ConnectedPieces)
            {
                ShiftSkeletonValues(track, track.Skeleton[pieces], newShift, pieces);
            }
        }
    }
}