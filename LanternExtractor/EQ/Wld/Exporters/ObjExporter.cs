using System.Collections.Generic;
using System.Text;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ObjExporter
    {
        /*StringBuilder _export = new StringBuilder();

        public void AddMeshData(List<vec3> vertices, List<vec2> uvs, List<Polygon> indices, List<RenderGroup> renderGroups, List<string> materials)
        {
            var frames = new List<string>();
            var usedVertices = new List<int>();
            var unusedVertices = new List<int>();

            int currentPolygon = 0;

            var faceOutput = new StringBuilder();

            // First assemble the faces that are needed
            foreach (RenderGroup group in renderGroups)
            {
                int textureIndex = group.TextureIndex;
                int polygonCount = group.PolygonCount;

                List<int> activeArray = null;
                //bool bitmapValid = false;

                if(MaterialList.Materials[textureIndex].ShaderType != ShaderType.Invisible)
                {
                    activeArray = usedVertices;
                }
                else
                {
                    activeArray = settings.ExportHiddenGeometry ? usedVertices : unusedVertices;
                }
                
                if (textureIndex < 0 || textureIndex >= MaterialList.Materials.Count)
                {
                    logger.LogError("Invalid texture index");
                    continue;
                }

                string filenameWithoutExtension = MaterialList.Materials[textureIndex].GetFirstBitmapNameWithoutExtension();

                string textureChange = string.Empty;
                
                if(MaterialList.Materials[textureIndex].ShaderType != ShaderType.Invisible
                    || (MaterialList.Materials[textureIndex].ShaderType == ShaderType.Invisible && settings.ExportHiddenGeometry))
                {
                    // Material change
                    if (activeMaterial != MaterialList.Materials[textureIndex])
                    {
                        if (string.IsNullOrEmpty(filenameWithoutExtension))
                        {
                            textureChange = LanternStrings.ObjUseMtlPrefix + "null";
                        }
                        else
                        {
                            string materialPrefix =
                                    MaterialList.GetMaterialPrefix(MaterialList.Materials[textureIndex].ShaderType);
                            textureChange = LanternStrings.ObjUseMtlPrefix + materialPrefix + filenameWithoutExtension;
                        }
                        
                        activeMaterial = MaterialList.Materials[textureIndex];
                    }
                }

                for (int j = 0; j < polygonCount; ++j)
                {
                    if(currentPolygon < 0 || currentPolygon >= Polygons.Count)
                    {
                        logger.LogError("Invalid polygon index");
                        continue;
                    }
                    
                    // This is the culprit.
                    if (!Polygons[currentPolygon].Solid && objExportType == ObjExportType.Collision)
                    {
                        activeArray = unusedVertices;
                        AddIfNotContained(activeArray, Polygons[currentPolygon].Vertex1);
                        AddIfNotContained(activeArray, Polygons[currentPolygon].Vertex2);
                        AddIfNotContained(activeArray, Polygons[currentPolygon].Vertex3);

                        currentPolygon++;
                        continue;
                    }
                    
                    if(textureChange != string.Empty)
                    {
                        faceOutput.AppendLine(textureChange);
                        textureChange = string.Empty;
                    }

                    int vertex1 = Polygons[currentPolygon].Vertex1 + baseVertex + 1;
                    int vertex2 = Polygons[currentPolygon].Vertex2 + baseVertex + 1;
                    int vertex3 = Polygons[currentPolygon].Vertex3 + baseVertex + 1;

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
        }*/
    }
}