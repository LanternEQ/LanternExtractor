using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshObjExporter : TextAssetExporter
    {
        private Material _activeMaterial;
        private ObjExportType _objExportType;
        private bool _exportHiddenGeometry;
        private int _usedVertices;
        private int _baseVertex;
        private Material _lastMaterial;
        private bool _isFirstMesh = true;
        private string _zoneName;

        private List<StringBuilder> _frames = new List<StringBuilder>();
        
        public MeshObjExporter(ObjExportType exportType, bool exportHiddenGeometry, string zoneName) : base()
        {
            _objExportType = exportType;
            _exportHiddenGeometry = exportHiddenGeometry;
            _zoneName = zoneName;
        }

        private bool _hasCollisionModel = false;

        public override void AddFragmentData(WldFragment fragment)
        {
            Mesh mesh = fragment as Mesh;

            if (mesh == null)
            {
                return;
            }

            if (_isFirstMesh)
            {
                _export.AppendLine(LanternStrings.ObjMaterialHeader + _zoneName + ".mtl");
                _isFirstMesh = false;
            }

            if (mesh.ExportSeparateCollision)
            {
                _hasCollisionModel = true;
            }

            var frames = new List<string>();
            var usedVertices = new List<int>();
            var unusedVertices = new List<int>();

            int currentPolygon = 0;

            var faceOutput = new StringBuilder();

            // First assemble the faces that are needed
            foreach (RenderGroup group in mesh.RenderGroups)
            {
                int textureIndex = group.TextureIndex;
                int polygonCount = group.PolygonCount;

                List<int> activeArray = null;
                //bool bitmapValid = false;

                if(mesh.MaterialList.Materials[textureIndex].ShaderType != ShaderType.Invisible)
                {
                    activeArray = usedVertices;
                }
                else
                {
                    activeArray = _exportHiddenGeometry ? usedVertices : unusedVertices;
                }

                if (textureIndex < 0 || textureIndex >= mesh.MaterialList.Materials.Count)
                {
                    //logger.LogError("Invalid texture index");
                    continue;
                }

                string filenameWithoutExtension = mesh.MaterialList.Materials[textureIndex].GetFirstBitmapNameWithoutExtension();

                string textureChange = string.Empty;
                
                if(mesh.MaterialList.Materials[textureIndex].ShaderType != ShaderType.Invisible
                    || (mesh.MaterialList.Materials[textureIndex].ShaderType == ShaderType.Invisible && _exportHiddenGeometry))
                {
                    // Material change
                    if (_activeMaterial != mesh.MaterialList.Materials[textureIndex])
                    {
                        if (string.IsNullOrEmpty(filenameWithoutExtension))
                        {
                            textureChange = LanternStrings.ObjUseMtlPrefix 
                                            + "null";
                        }
                        else
                        {
                            string materialPrefix =
                                    MaterialList.GetMaterialPrefix(mesh.MaterialList.Materials[textureIndex].ShaderType);
                            textureChange = LanternStrings.ObjUseMtlPrefix + materialPrefix + filenameWithoutExtension;
                        }
                        
                        _activeMaterial = mesh.MaterialList.Materials[textureIndex];
                    }
                }

                for (int j = 0; j < polygonCount; ++j)
                {
                    if(currentPolygon < 0 || currentPolygon >= mesh.Indices.Count)
                    {
                        //logger.LogError("Invalid polygon index");
                        continue;
                    }
                    
                    // This is the culprit.
                    if (!mesh.Indices[currentPolygon].Solid && _objExportType == ObjExportType.Collision)
                    {
                        activeArray = unusedVertices;
                        AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex1);
                        AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex2);
                        AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex3);

                        currentPolygon++;
                        continue;
                    }
                    
                    if(textureChange != string.Empty)
                    {
                        faceOutput.AppendLine(textureChange);
                        textureChange = string.Empty;
                    }

                    int vertex1 = mesh.Indices[currentPolygon].Vertex1 + _baseVertex + 1;
                    int vertex2 = mesh.Indices[currentPolygon].Vertex2 + _baseVertex + 1;
                    int vertex3 = mesh.Indices[currentPolygon].Vertex3 + _baseVertex + 1;

                    if (activeArray == usedVertices)
                    {
                        int index1 = vertex1 - unusedVertices.Count;
                        int index2 = vertex2 - unusedVertices.Count;
                        int index3 = vertex3 - unusedVertices.Count;

                        // Vertex + UV
                        if (_objExportType != ObjExportType.Collision)
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

                    AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex1);
                    AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex2);
                    AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex3);

                    currentPolygon++;
                }
            }

            var vertexOutput = new StringBuilder();

            usedVertices.Sort();

            int frameCount = 1;

            if (mesh.AnimatedVertices != null)
            {
                frameCount += mesh.AnimatedVertices.Frames.Count;
            }

            for (int i = 0; i < frameCount; ++i)
            {
                // Add each vertex
                foreach (var usedVertex in usedVertices)
                {
                    vec3 vertex;

                    if (i == 0)
                    {
                        if(usedVertex < 0 || usedVertex >= mesh.Vertices.Count)
                        {
                            //logger.LogError("Invalid vertex index: " + usedVertex);
                            continue;
                        }

                        vertex = mesh.Vertices[usedVertex];
                    }
                    else
                    {
                        if (mesh.AnimatedVertices == null)
                        {
                            continue;
                        }

                        vertex = mesh.AnimatedVertices.Frames[i - 1][usedVertex];
                    }

                    vertexOutput.AppendLine("v " + -(vertex.x + mesh.Center.x) + " " + (vertex.z + mesh.Center.z) + " " +
                                            (vertex.y + mesh.Center.y));

                    if (_objExportType == ObjExportType.Collision)
                    {
                        continue;
                    }

                    if(usedVertex >= mesh.TextureUvCoordinates.Count)
                    {
                        vertexOutput.AppendLine("vt " + 0.0f + " " + 0.0f);

                        continue;
                    }

                    vec2 vertexUvs = mesh.TextureUvCoordinates[usedVertex];
                    vertexOutput.AppendLine("vt " + vertexUvs.x + " " + vertexUvs.y);
                }

                frames.Add(vertexOutput.ToString() + faceOutput);
                vertexOutput.Clear();
            }

            _lastMaterial = _activeMaterial;

            // Ensure that output use the decimal point rather than the comma (as in Germany)
            for (var i = 0; i < frames.Count; i++)
            {
                frames[i] = frames[i].Replace(',', '.');

                if (i == 0)
                {
                    _export.Append(frames[i]);
                }
                else
                {
                    _frames.Add(new StringBuilder());
                    _frames.Last().Append(frames[i]);
                }
            }

            _baseVertex += usedVertices.Count;
        }
        
        private void AddIfNotContained(List<int> list, int element)
        {
            if (list.Contains(element))
            {
                return;
            }

            list.Add(element);
        }

        public override void WriteAssetToFile(string fileName)
        {
            if (!_hasCollisionModel)
            {
                return;
            }
            
            base.WriteAssetToFile(fileName);
        }

        public void WriteAllFrames(string fileName)
        {
            if (_frames.Count == 1)
            {
                return;
            }

            for (int i = 1; i < _frames.Count; ++i)
            {
                _export = _frames[i];
                WriteAssetToFile(fileName.Replace(".obj", "") + "_frame" + i + ".obj");
            }
        }
    }
}