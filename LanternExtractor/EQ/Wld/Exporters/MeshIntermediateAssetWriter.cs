using System.Collections.Generic;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshIntermediateAssetWriter : TextAssetWriter
    {
        private bool _useGroups;
        private bool _isCollisionMesh;
        private bool _isFirstMesh = true;
        private int _currentBaseIndex;

        public override void ClearExportData()
        {
            base.ClearExportData();
            _isFirstMesh = true;
            _currentBaseIndex = 0;
        }

        public MeshIntermediateAssetWriter(bool useGroups, bool isCollisionMesh)
        {
            _useGroups = useGroups;
            _isCollisionMesh = isCollisionMesh;
            _export.AppendLine("# Lantern Test Intermediate Format");
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            Mesh mesh = data as Mesh;

            if (mesh == null)
            {
                return;
            }
            
            HashSet<int> usedVertices = new HashSet<int>();
            List<Polygon> newIndices = new List<Polygon>();

            int currentPolygon = 0;

            foreach (RenderGroup group in mesh.MaterialGroups)
            {
                for (int i = 0; i < group.PolygonCount; ++i)
                {
                    Polygon polygon = mesh.Indices[currentPolygon];
                    
                    newIndices.Add(polygon.GetCopy());
                    currentPolygon++;

                    if (!polygon.IsSolid && _isCollisionMesh)
                    {
                        continue;
                    }
                    
                    usedVertices.Add(polygon.Vertex1);
                    usedVertices.Add(polygon.Vertex2);
                    usedVertices.Add(polygon.Vertex3);
                }
            }

            // Get rid of this hack
            if (!_isCollisionMesh)
            {
                usedVertices.Clear();

                for (int i = 0; i < mesh.Vertices.Count; ++i)
                {
                    usedVertices.Add(i);
                }
            }

            int unusedVertices = 0;
            for (int i = mesh.Vertices.Count - 1; i >= 0; i--)
            {
                if (usedVertices.Contains(i))
                {
                    continue;
                }

                unusedVertices++;
                
                foreach (var polygon in newIndices)
                {
                    if (polygon.Vertex1 >= i && polygon.Vertex1 != 0)
                    {
                        polygon.Vertex1--;
                    }
                    if (polygon.Vertex2 >= i && polygon.Vertex2 != 0)
                    {
                        polygon.Vertex2--;
                    }
                    if (polygon.Vertex3 >= i && polygon.Vertex3 != 0)
                    {
                        polygon.Vertex3--;
                    }
                }
            }

            if (!_isCollisionMesh && (_isFirstMesh || _useGroups))
            {
                _export.Append("ml");
                _export.Append(",");
                _export.Append(FragmentNameCleaner.CleanName(mesh.MaterialList));
                _export.AppendLine();
                _isFirstMesh = false;
            }

            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                if (!usedVertices.Contains(i))
                {
                    continue;
                }
                
                var vertex = mesh.Vertices[i];
                _export.Append("v");
                _export.Append(",");
                _export.Append(vertex.x + mesh.Center.x);
                _export.Append(",");
                _export.Append(vertex.z + mesh.Center.z);
                _export.Append(",");
                _export.Append(vertex.y + mesh.Center.y);
                _export.AppendLine();
            }

            for (var i = 0; i < mesh.TextureUvCoordinates.Count; i++)
            {
                if (!usedVertices.Contains(i) || _isCollisionMesh)
                {
                    continue;
                }
                
                var textureUv = mesh.TextureUvCoordinates[i];
                _export.Append("uv");
                _export.Append(",");
                _export.Append(textureUv.x);
                _export.Append(",");
                _export.Append(textureUv.y);
                _export.AppendLine();
            }

            for (var i = 0; i < mesh.Normals.Count; i++)
            {
                if (!usedVertices.Contains(i) || _isCollisionMesh)
                {
                    continue;
                }
                
                var normal = mesh.Normals[i];
                _export.Append("n");
                _export.Append(",");
                _export.Append(normal.x);
                _export.Append(",");
                _export.Append(normal.y);
                _export.Append(",");
                _export.Append(normal.z);
                _export.AppendLine();
            }

            for (var i = 0; i < mesh.Colors.Count; i++)
            {
                if (!usedVertices.Contains(i) || _isCollisionMesh)
                {
                    continue;
                }
                
                var vertexColor = mesh.Colors[i];
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

            currentPolygon = 0;

            foreach (RenderGroup group in mesh.MaterialGroups)
            {
                if (!_isCollisionMesh)
                {
                    _export.Append("mg");
                    _export.Append(",");
                    _export.Append(group.MaterialIndex - mesh.StartTextureIndex);
                    _export.Append(",");
                    _export.Append(group.PolygonCount);
                    _export.AppendLine();
                }

                for (int i = 0; i < group.PolygonCount; ++i)
                {
                    Polygon polygon = newIndices[currentPolygon];
                    
                    currentPolygon++;
                    
                    _export.Append("i");
                    _export.Append(",");
                    _export.Append(group.MaterialIndex);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex1);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex2);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex3);
                    _export.AppendLine();
                }
            }
            
            foreach (var bone in mesh.MobPieces)
            {
                _export.Append("b");
                _export.Append(",");
                _export.Append(bone.Key);
                _export.Append(",");
                _export.Append(bone.Value.Start);
                _export.Append(",");
                _export.Append(bone.Value.Count);
                _export.AppendLine();
            }

            if (mesh.AnimatedVerticesReference != null && !_isCollisionMesh)
            {
                _export.Append("ad");
                _export.Append(",");
                _export.Append(mesh.AnimatedVerticesReference.MeshAnimatedVertices.Delay);
                _export.AppendLine();

                for (var i = 0; i < mesh.AnimatedVerticesReference.MeshAnimatedVertices.Frames.Count; i++)
                {
                    List<vec3> frame = mesh.AnimatedVerticesReference.MeshAnimatedVertices.Frames[i];
                    foreach (vec3 position in frame)
                    {
                        _export.Append("av");
                        _export.Append(",");
                        _export.Append(i);
                        _export.Append(",");
                        _export.Append(position.x + mesh.Center.x);
                        _export.Append(",");
                        _export.Append(position.z + mesh.Center.z);
                        _export.Append(",");
                        _export.Append(position.y + mesh.Center.y);
                        _export.AppendLine();
                    }
                }
            }

            if (!_useGroups)
            {
                _currentBaseIndex += mesh.Vertices.Count - unusedVertices;
            }
        }
    }
}