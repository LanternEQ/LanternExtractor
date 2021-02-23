using System;
using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public class AlternateMesh : WldFragment
    {
        public List<vec3> Vertices = new List<vec3>();
        public List<vec2> TexCoords = new List<vec2>();
        public List<vec3> Normals = new List<vec3>();
        public List<Polygon> Polygons = new List<Polygon>();
        public List<ivec2> VertexTex = new List<ivec2>();
        public List<RenderGroup> RenderGroups = new List<RenderGroup>();
        public MaterialList MaterialList;
        public Dictionary<int, MobVertexPiece> MobPieces { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data, List<WldFragment> fragments, Dictionary<int, string> stringHash,
            bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            
            var reader = new BinaryReader(new MemoryStream(data));
            Name = stringHash[-reader.ReadInt32()];
            int flags = reader.ReadInt32();
            int vertexCount = reader.ReadInt32();
            int texCoordCount = reader.ReadInt32();
            int normalsCount = reader.ReadInt32();
            int colorsCount = reader.ReadInt32(); // size4
            int polygonCount = reader.ReadInt32();
            int size6 = reader.ReadInt16();
            int fragment1maybe = reader.ReadInt16();
            int vertexPieceCount = reader.ReadInt32(); // -1
            int materialList = reader.ReadInt32();
            MaterialList = fragments[materialList - 1] as MaterialList;
            int fragment3 = reader.ReadInt32();
            float centerX = reader.ReadSingle();
            float centerY = reader.ReadSingle();
            float centerZ = reader.ReadSingle();
            int params2 = reader.ReadInt32();
            int something2 = reader.ReadInt32();
            float something3 = reader.ReadInt32();
            
            // vertex entries
            for (int i = 0; i < vertexCount; ++i)
            {
                Vertices.Add(new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }
            
            for (int i = 0; i < texCoordCount; ++i)
            {
                TexCoords.Add(new vec2(reader.ReadSingle(), reader.ReadSingle()));
            }
            
            for (int i = 0; i < normalsCount; ++i)
            {
                Normals.Add(new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }

            reader.BaseStream.Position += colorsCount * sizeof(int);
            
            for (int i = 0; i < polygonCount; ++i)
            {
                int flag = reader.ReadInt16();
                
                int unk1 = reader.ReadInt16();
                int unk2 = reader.ReadInt16();
                int unk3 = reader.ReadInt16();
                int unk4 = reader.ReadInt16();

                int i1 = reader.ReadInt16();
                int i2 = reader.ReadInt16();
                int i3 = reader.ReadInt16();
                Polygons.Add(new Polygon {IsSolid = true, Vertex1 = i1, Vertex2 = i2, Vertex3 = i3});
            }
            
            if (Name.ToLower().Contains("crate"))
            {
                
            }
            
            for (int i = 0; i < size6; ++i)
            {
                
                int datatype = reader.ReadInt32();
                
                if (datatype != 4)
                {
                    int vertexIndex = reader.ReadInt32();
                    int data6Param1 = reader.ReadInt16();
                    int data6Param2 = reader.ReadInt16();
                }
                else
                {
                    float offset = reader.ReadSingle();
                    int something = reader.ReadInt32();
                    
                    if ((int) offset == 7)
                    {
                        
                    }
                }
            }
            
            MobPieces = new Dictionary<int, MobVertexPiece>();
            int mobStart = 0;
            for (int i = 0; i < vertexPieceCount; ++i)
            {
                var mobVertexPiece = new MobVertexPiece
                {
                    Count = reader.ReadInt16(),
                    Start = reader.ReadInt16()
                };

                mobStart += mobVertexPiece.Count;

                MobPieces[mobVertexPiece.Start] = mobVertexPiece;
            }

            // ?? what this is? -- only sometimes working
            //reader.BaseStream.Position += 4;

            BitAnalyzer ba = new BitAnalyzer(flags);

            if (ba.IsBitSet(9))
            {
                int size8 = reader.ReadInt32();

                reader.BaseStream.Position += size8 * 4;
            }
            
            if (ba.IsBitSet(11))
            {
                int polygonTexCount = reader.ReadInt32();

                if (polygonTexCount == 65565)
                {
                    polygonTexCount = reader.ReadInt32();
                }

                for (int i = 0; i < polygonTexCount; ++i)
                {
                    RenderGroups.Add(new RenderGroup
                    {
                        PolygonCount = reader.ReadInt16(),
                        MaterialIndex = reader.ReadInt16(),
                    });
                }
            }
            
            if (ba.IsBitSet(12))
            {
                int vertexTexCount = reader.ReadInt32();

                for (int i = 0; i < vertexTexCount; ++i)
                {
                    VertexTex.Add(new ivec2
                    {
                        x = reader.ReadInt16(),
                        y = reader.ReadInt16()
                    });
                }
            }

            return;
            

            
            if (ba.IsBitSet(13))
            {
                int params3_1 = reader.ReadInt32();
                int params3_2 = reader.ReadInt32();
                int params3_3 = reader.ReadInt32();
            }
        }
    }
}