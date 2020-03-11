using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshIntermediateExporter : TextAssetExporter
    {
        private int _currentBaseIndex;

        public MeshIntermediateExporter()
        {
            _export.AppendLine("# Lantern Test Intermediate Format");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            Mesh mesh = data as Mesh;

            if (mesh == null)
            {
                return;
            }
            
            foreach (var vertex in mesh.Vertices)
            {
                _export.Append("v");
                _export.Append(",");
                _export.Append(vertex.x + mesh.Center.x);
                _export.Append(",");
                _export.Append(vertex.z + mesh.Center.z);
                _export.Append(",");
                _export.Append(vertex.y + mesh.Center.y);
                _export.AppendLine();
            }
            
            foreach (var textureUv in mesh.TextureUvCoordinates)
            {
                _export.Append("uv");
                _export.Append(",");
                _export.Append(textureUv.x);
                _export.Append(",");
                _export.Append(textureUv.y);
                _export.AppendLine();
            }
            
            foreach (var normal in mesh.Normals)
            {
                _export.Append("n");
                _export.Append(",");
                _export.Append(normal.x);
                _export.Append(",");
                _export.Append(normal.y);
                _export.Append(",");
                _export.Append(normal.z);
                _export.AppendLine();
            }

            foreach (var vertexColor in mesh.Colors)
            {
                _export.Append("c");
                _export.Append(",");
                _export.Append(vertexColor.B);
                _export.Append(",");
                _export.Append(vertexColor.G);
                _export.Append(",");
                _export.Append(vertexColor.R);
                _export.Append(",");
                _export.Append(vertexColor.A);
                _export.AppendLine();
            }

            int currentPolygon = 0;

            foreach (RenderGroup group in mesh.MaterialGroups)
            {
                string materialName = string.Empty;
                
                _export.Append("mg");
                _export.Append(",");
                _export.Append(group.MaterialIndex - mesh.StartTextureIndex);
                _export.Append(",");
                _export.Append(group.PolygonCount);
                _export.AppendLine();
                
                for (int i = 0; i < group.PolygonCount; ++i)
                {
                    int vertex1 = mesh.Indices[currentPolygon].Vertex1;
                    int vertex2 = mesh.Indices[currentPolygon].Vertex2;
                    int vertex3 = mesh.Indices[currentPolygon].Vertex3;

                    _export.Append("i");
                    _export.Append(",");
                    _export.Append(group.MaterialIndex);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + vertex1);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + vertex2);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + vertex3);
                    _export.AppendLine();
                    currentPolygon++;
                }
            }
            
            _currentBaseIndex += mesh.Vertices.Count;
        }
    }
}