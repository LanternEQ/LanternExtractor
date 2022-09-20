using System.Collections.Generic;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// LegacyMesh (0x2C)
    /// Internal name: _DMSPRITEDEF
    /// This fragment is only found in the gequip archives and while it exists and is functional, it is not used.
    /// It looks like an earlier version of the Mesh fragment with fewer data points.
    /// </summary>
    public class LegacyMesh : WldFragment
    {
        public List<vec3> Vertices = new List<vec3>();
        public List<vec2> TexCoords = new List<vec2>();
        public List<vec3> Normals = new List<vec3>();
        public List<Polygon> Polygons = new List<Polygon>();
        public List<ivec2> VertexTex = new List<ivec2>();
        public List<RenderGroup> RenderGroups = new List<RenderGroup>();
        public MaterialList MaterialList;
        public Dictionary<int, MobVertexPiece> MobPieces { get; private set; }

        public override void Initialize(int index, int size, byte[] data, List<WldFragment> fragments, Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            int flags = Reader.ReadInt32();
            int vertexCount = Reader.ReadInt32();
            int texCoordCount = Reader.ReadInt32();
            int normalsCount = Reader.ReadInt32();
            int colorsCount = Reader.ReadInt32(); // size4
            int polygonCount = Reader.ReadInt32();
            int size6 = Reader.ReadInt16();
            int fragment1maybe = Reader.ReadInt16();
            int vertexPieceCount = Reader.ReadInt32(); // -1
            MaterialList = fragments[Reader.ReadInt32() - 1] as MaterialList;
            int fragment3 = Reader.ReadInt32();
            float centerX = Reader.ReadSingle();
            float centerY = Reader.ReadSingle();
            float centerZ = Reader.ReadSingle();
            int params2 = Reader.ReadInt32();
            int something2 = Reader.ReadInt32();
            float something3 = Reader.ReadInt32();
            
            for (int i = 0; i < vertexCount; ++i)
            {
                Vertices.Add(new vec3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle()));
            }
            
            for (int i = 0; i < texCoordCount; ++i)
            {
                TexCoords.Add(new vec2(Reader.ReadSingle(), Reader.ReadSingle()));
            }
            
            for (int i = 0; i < normalsCount; ++i)
            {
                Normals.Add(new vec3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle()));
            }

            Reader.BaseStream.Position += colorsCount * sizeof(int);
            
            for (int i = 0; i < polygonCount; ++i)
            {
                int flag = Reader.ReadInt16();
                
                int unk1 = Reader.ReadInt16();
                int unk2 = Reader.ReadInt16();
                int unk3 = Reader.ReadInt16();
                int unk4 = Reader.ReadInt16();

                int i1 = Reader.ReadInt16();
                int i2 = Reader.ReadInt16();
                int i3 = Reader.ReadInt16();
                Polygons.Add(new Polygon
                {
                    IsSolid = true, 
                    Vertex1 = i1, 
                    Vertex2 = i2, 
                    Vertex3 = i3
                });
            }
            
            for (int i = 0; i < size6; ++i)
            {
                
                int datatype = Reader.ReadInt32();
                
                if (datatype != 4)
                {
                    int vertexIndex = Reader.ReadInt32();
                    int data6Param1 = Reader.ReadInt16();
                    int data6Param2 = Reader.ReadInt16();
                }
                else
                {
                    float offset = Reader.ReadSingle();
                    int something = Reader.ReadInt32();
                }
            }
            
            MobPieces = new Dictionary<int, MobVertexPiece>();
            int mobStart = 0;
            for (int i = 0; i < vertexPieceCount; ++i)
            {
                var mobVertexPiece = new MobVertexPiece
                {
                    Count = Reader.ReadInt16(),
                    Start = Reader.ReadInt16()
                };

                mobStart += mobVertexPiece.Count;

                MobPieces[mobVertexPiece.Start] = mobVertexPiece;
            }
            
            BitAnalyzer ba = new BitAnalyzer(flags);

            if (ba.IsBitSet(9))
            {
                int size8 = Reader.ReadInt32();

                Reader.BaseStream.Position += size8 * 4;
            }
            
            if (ba.IsBitSet(11))
            {
                int polygonTexCount = Reader.ReadInt32();

                for (int i = 0; i < polygonTexCount; ++i)
                {
                    RenderGroups.Add(new RenderGroup
                    {
                        PolygonCount = Reader.ReadInt16(),
                        MaterialIndex = Reader.ReadInt16()
                    });
                }
            }
            
            if (ba.IsBitSet(12))
            {
                int vertexTexCount = Reader.ReadInt32();

                for (int i = 0; i < vertexTexCount; ++i)
                {
                    VertexTex.Add(new ivec2
                    {
                        x = Reader.ReadInt16(),
                        y = Reader.ReadInt16()
                    });
                }
            }
            
            if (ba.IsBitSet(13))
            {
                int params3_1 = Reader.ReadInt32();
                int params3_2 = Reader.ReadInt32();
                int params3_3 = Reader.ReadInt32();
            }
        }
    }
}