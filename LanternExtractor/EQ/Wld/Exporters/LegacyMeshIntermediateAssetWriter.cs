using System;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    /// <summary>
    /// Exports an alternate mesh in the intermediate mesh format
    /// </summary>
    public class LegacyMeshIntermediateAssetWriter : TextAssetWriter
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

        public LegacyMeshIntermediateAssetWriter(bool useGroups, bool isCollisionMesh)
        {
            _useGroups = useGroups;
            _isCollisionMesh = isCollisionMesh;
        }

        public override void AddFragmentData(WldFragment data)
        {
            if (!(data is LegacyMesh am))
            {
                return;
            }

            if (_isCollisionMesh && am.PolyhedronReference != null)
            {
                var polyhedron = am.PolyhedronReference.Polyhedron;

                // TODO: polyhedron scale factor
                foreach(var vertex in polyhedron.Vertices)
                {
                    _export.Append("v");
                    _export.Append(",");
                    _export.Append(vertex.x + am.Center.x);
                    _export.Append(",");
                    _export.Append(vertex.z + am.Center.z);
                    _export.Append(",");
                    _export.Append(vertex.y + am.Center.y);
                    _export.AppendLine();
                }

                foreach(var polygon in polyhedron.Faces)
                {
                    _export.Append("i");
                    _export.Append(",");
                    _export.Append(0);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex1);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex2);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex3);
                    _export.AppendLine();
                }

                return;
            }

            if (!_isCollisionMesh && (_isFirstMesh || _useGroups))
            {
                _export.Append("ml");
                _export.Append(",");
                _export.Append(FragmentNameCleaner.CleanName(am.MaterialList));
                _export.AppendLine();
                _isFirstMesh = false;
            }

            foreach (var vertex in am.Vertices)
            {
                _export.Append("v");
                _export.Append(",");
                _export.Append(vertex.x + am.Center.x);
                _export.Append(",");
                _export.Append(vertex.z + am.Center.z);
                _export.Append(",");
                _export.Append(vertex.y + am.Center.y);
                _export.AppendLine();
            }

            foreach (var uv in am.TexCoords)
            {
                _export.Append("uv");
                _export.Append(",");
                _export.Append(uv.x);
                _export.Append(",");
                _export.Append(uv.y);
                _export.AppendLine();
            }

            foreach (var normal in am.Normals)
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

            int currentPolygon = 0;
            for (var i = 0; i < am.RenderGroups.Count; i++)
            {
                var renderGroup = am.RenderGroups[i];
                for (int j = 0; j < renderGroup.PolygonCount; ++j)
                {
                    Polygon polygon = am.Polygons[currentPolygon];
                    currentPolygon++;

                    _export.Append("i");
                    _export.Append(",");
                    _export.Append(renderGroup.MaterialIndex);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex1);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex2);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex3);
                    _export.AppendLine();
                }
            }

            if (am.RenderGroups.Count == 0)
            {
                foreach (var polygon in am.Polygons)
                {
                    _export.Append("i");
                    _export.Append(",");
                    _export.Append(polygon.MaterialIndex);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex1);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex2);
                    _export.Append(",");
                    _export.Append(_currentBaseIndex + polygon.Vertex3);
                    _export.AppendLine();
                }
            }

            foreach (var bone in am.MobPieces)
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

            var animatedVertices = am.AnimatedVerticesReference?.GetAnimatedVertices();
            if (animatedVertices != null && !_isCollisionMesh)
            {
                _export.Append("ad");
                _export.Append(",");
                _export.Append(animatedVertices.Delay);
                _export.AppendLine();

                for (var i = 0; i < animatedVertices.Frames.Count; i++)
                {
                    foreach (vec3 position in animatedVertices.Frames[i])
                    {
                        _export.Append("av");
                        _export.Append(",");
                        _export.Append(i);
                        _export.Append(",");
                        _export.Append(position.x + am.Center.x);
                        _export.Append(",");
                        _export.Append(position.z + am.Center.z);
                        _export.Append(",");
                        _export.Append(position.y + am.Center.y);
                        _export.AppendLine();
                    }
                }
            }

            if (!_useGroups)
            {
                _currentBaseIndex += am.Vertices.Count;
            }
        }

        public override void WriteAssetToFile(string fileName)
        {
            if (_export.Length == 0)
            {
                return;
            }

            _export.Insert(0, LanternStrings.ExportHeaderTitle + "Alternate Mesh Intermediate Format" + Environment.NewLine);

            base.WriteAssetToFile(fileName);
        }
    }
}
