using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    /// <summary>
    /// Exports an alternate mesh in the intermediate mesh format
    /// </summary>
    public class AlternateMeshIntermediateAssetWriter : TextAssetWriter
    {
        public override void AddFragmentData(WldFragment data)
        {
            if (!(data is LegacyMesh am))
            {
                return;
            }
            
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Alternate Mesh Intermediate Format");
            _export.AppendLine($"ml,{FragmentNameCleaner.CleanName(am.MaterialList)}");

            foreach (var v in am.Vertices)
            {
                _export.Append("v");
                _export.Append(",");
                _export.Append(v.x);
                _export.Append(",");
                _export.Append(v.z);
                _export.Append(",");
                _export.Append(v.y);
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
            
            foreach (var n in am.Normals)
            {
                _export.Append("n");
                _export.Append(",");
                _export.Append(n.x);
                _export.Append(",");
                _export.Append(n.y);
                _export.Append(",");
                _export.Append(n.z);
                _export.AppendLine();
            }

            int currentPolygon = 0;
            for (var i = 0; i < am.RenderGroups.Count; i++)
            {
                var renderGroup = am.RenderGroups[i];
                for (int j = 0; j < renderGroup.PolygonCount; ++j)
                {
                    Polygon polygon = am.Polygons[j + currentPolygon];
                    
                    _export.Append("i");
                    _export.Append(",");
                    _export.Append(renderGroup.MaterialIndex);
                    _export.Append(",");
                    _export.Append(polygon.Vertex1);
                    _export.Append(",");
                    _export.Append(polygon.Vertex2);
                    _export.Append(",");
                    _export.Append(polygon.Vertex3);
                    _export.AppendLine();
                }

                currentPolygon += renderGroup.PolygonCount;
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
        }
    }
}