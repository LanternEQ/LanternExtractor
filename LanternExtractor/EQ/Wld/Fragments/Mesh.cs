using System;
using System.Collections.Generic;
using System.IO;
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
        public MeshAnimatedVerticesReference AnimatedVerticesReference { get; private set; }

        /// <summary>
        /// Set to true if there are non solid polygons in the mesh
        /// This means we export collision separately (e.g. trees, fire)
        /// </summary>
        public bool ExportSeparateCollision { get; private set; }

        public bool IsHandled = false;

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
            else
            {
                
            }
            
            int textureList = reader.ReadInt32();

            MaterialList = fragments[textureList - 1] as MaterialList;

            int meshAnimation = reader.ReadInt32();

            // Vertex animation only
            if (meshAnimation != 0)
            {
                AnimatedVerticesReference = fragments[meshAnimation - 1] as MeshAnimatedVerticesReference;
            }

            int unknown = reader.ReadInt32();

            // maybe references the first 0x03 in the WLD - unknown
            int unknown2 = reader.ReadInt32();

            Center = new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            // 3 unknown dwords
            int unknownDword1 = reader.ReadInt32();
            int unknownDword2 = reader.ReadInt32();
            int unknownDword3 = reader.ReadInt32();

            // Seems to be related to lighting models? (torches, etc.)
            if (unknownDword1 != 0 || unknownDword2 != 0 || unknownDword3 != 0)
            {
                
            }
            
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

            if (size9 != 0)
            {
                
            }

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
                    IsSolid = isSolid,
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
            logger.LogInfo("Mesh: Center: " + Center);
            logger.LogInfo("Mesh: Max distance: " + MaxDistance);
            logger.LogInfo("Mesh: Min position: " + MinPosition);
            logger.LogInfo("Mesh: Max position: " + MaxDistance);
            logger.LogInfo("Mesh: Texture list reference: " + MaterialList.Index);
            logger.LogInfo("Mesh: Vertex count: " + Vertices.Count);
            logger.LogInfo("Mesh: Polygon count: " + Indices.Count);
            logger.LogInfo("Mesh: Texture coordinate count: " + TextureUvCoordinates.Count);
            logger.LogInfo("Mesh: Render group count: " + MaterialGroups.Count);
            logger.LogInfo("Mesh: Export separate collision: " + ExportSeparateCollision);

            if (AnimatedVerticesReference != null)
            {
                logger.LogInfo("Mesh: Animated mesh vertices reference: " + AnimatedVerticesReference.Index);
            }
        }
    }
}