using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshObjWriter : TextAssetWriter
    {
        /// <summary>
        /// Is this the first mesh in the export?
        /// Zones are made up of multiple meshes. The OBJ header is only added when this is set.
        /// </summary>
        private bool _isFirstMesh = true;

        /// <summary>
        /// The currently active material
        /// As zones are made of multiple meshes, this prevents multiple usemtl declarations of the same material
        /// </summary>
        private Material _activeMaterial;
        
        /// <summary>
        /// Used when dealing with multiple meshes
        /// The base vertex is added to submesh vertex to get the correct vertex value
        /// </summary>
        private int _baseVertex;
        
        /// <summary>
        /// If we export groups, the mesh is divided into submeshes.
        /// Only applies to the zone mesh.
        /// </summary>
        private bool _exportGroups;

        /// <summary>
        /// If true, invisible and boundary surfaces will be exported as well
        /// Only applies to the zone mesh.
        /// </summary>
        private bool _exportHiddenGeometry;
        
        private ObjExportType _objExportType;
        private int _usedVertices;
        private string _forcedMeshList;

        private List<StringBuilder> _frames = new List<StringBuilder>();

        public MeshObjWriter(ObjExportType exportType, bool exportHiddenGeometry, bool exportGroups, string zoneName, string forcedMeshList = "")
        {
            _objExportType = exportType;
            _exportHiddenGeometry = exportHiddenGeometry;
            _exportGroups = exportGroups;
            _forcedMeshList = forcedMeshList;
        }

        private bool _isCharacterModel;
        private bool _hasCollisionModel = false;

        public void SetIsCharacterModel(bool state)
        {
            _isCharacterModel = state;
        }

        public override void AddFragmentData(WldFragment fragment)
        {
            Mesh mesh = fragment as Mesh;

            if (mesh == null)
            {
                return;
            }

            // We only add the header if it's the first mesh
            // Zones, for example are made up of several smaller meshes
            if (_isFirstMesh && _objExportType == ObjExportType.Textured)
            {
                string name = LanternStrings.ObjMaterialHeader + FragmentNameCleaner.CleanName(mesh.MaterialList) +
                              (_isCharacterModel ? "_0" : string.Empty) +
                              ".mtl";

                if (!string.IsNullOrEmpty(_forcedMeshList))
                {
                    name = LanternStrings.ObjMaterialHeader + _forcedMeshList + ".mtl";
                }
                
                _export.AppendLine(name);
                _isFirstMesh = false;
            }
            
            if (_exportGroups)
            {
                _export.AppendLine("g " + FragmentNameCleaner.CleanName(mesh));
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
            foreach (RenderGroup group in mesh.MaterialGroups)
            {
                int textureIndex = group.MaterialIndex;
                int polygonCount = group.PolygonCount;

                List<int> activeArray;

                bool shouldExport = true;

                if (mesh.MaterialList.Materials[textureIndex].ShaderType == ShaderType.Boundary ||
                    mesh.MaterialList.Materials[textureIndex].ShaderType == ShaderType.Invisible)
                {
                    if (_objExportType != ObjExportType.Collision || !_exportHiddenGeometry)
                    {
                        shouldExport = false;
                    }
                }

                activeArray = shouldExport ? usedVertices : unusedVertices;

                if (textureIndex < 0 || textureIndex >= mesh.MaterialList.Materials.Count)
                {
                    continue;
                }

                string filenameWithoutExtension =
                    mesh.MaterialList.Materials[textureIndex].GetFirstBitmapNameWithoutExtension();

                string textureChange = string.Empty;

                if (shouldExport)
                {
                    // Material change
                    if (_activeMaterial != mesh.MaterialList.Materials[textureIndex] && _objExportType == ObjExportType.Textured)
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
                    if (currentPolygon < 0 || currentPolygon >= mesh.Indices.Count)
                    {
                        //logger.LogError("Invalid polygon index");
                        continue;
                    }

                    // This is the culprit.
                    if (!mesh.Indices[currentPolygon].IsSolid && _objExportType == ObjExportType.Collision)
                    {
                        activeArray = unusedVertices;
                        AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex1);
                        AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex2);
                        AddIfNotContained(activeArray, mesh.Indices[currentPolygon].Vertex3);

                        currentPolygon++;
                        continue;
                    }

                    if (textureChange != string.Empty)
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
            
            usedVertices.Clear();

            for (int i = 0; i < mesh.Vertices.Count; ++i)
            {
                usedVertices.Add(i);
            }

            int frameCount = 1;

            if (mesh.AnimatedVerticesReference != null)
            {
                MeshAnimatedVertices animatedVertices = mesh.AnimatedVerticesReference.MeshAnimatedVertices;
                
                frameCount += mesh.AnimatedVerticesReference.MeshAnimatedVertices.Frames.Count;
            }

            for (int i = 0; i < frameCount; ++i)
            {
                // Add each vertex
                foreach (var usedVertex in usedVertices)
                {
                    vec3 vertex;

                    if (i == 0)
                    {
                        if (usedVertex < 0 || usedVertex >= mesh.Vertices.Count)
                        {
                            //logger.LogError("Invalid vertex index: " + usedVertex);
                            continue;
                        }

                        vertex = mesh.Vertices[usedVertex];
                    }
                    else
                    {
                        if (mesh.AnimatedVerticesReference == null)
                        {
                            continue;
                        }

                        vertex = mesh.AnimatedVerticesReference.MeshAnimatedVertices.Frames[i - 1][usedVertex];
                    }

                    vertexOutput.AppendLine("v " + (-(vertex.x + mesh.Center.x)).ToString(_numberFormat) + " " +
                                            (vertex.z + mesh.Center.z).ToString(_numberFormat) + " " +
                                            (vertex.y + mesh.Center.y).ToString(_numberFormat));

                    if (_objExportType == ObjExportType.Collision)
                    {
                        continue;
                    }

                    if (usedVertex >= mesh.TextureUvCoordinates.Count)
                    {
                        vertexOutput.Append("vt " + 0.0f + " " + 0.0f);

                        continue;
                    }

                    vec2 vertexUvs = mesh.TextureUvCoordinates[usedVertex];
                    vertexOutput.AppendLine("vt " + vertexUvs.x.ToString(_numberFormat) + " " +
                                            vertexUvs.y.ToString(_numberFormat));
                }

                frames.Add(vertexOutput.ToString() + faceOutput);
                vertexOutput.Clear();
            }

            for (var i = 0; i < frames.Count; i++)
            {
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

            if (mesh.Name.Contains("BAT"))
            {
                
            }
        }

        private void AddIfNotContained(List<int> list, int element)
        {
            if (list.Contains(element))
            {
                return;
            }

            list.Add(element);
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

        public override void WriteAssetToFile(string fileName)
        {
            if (_objExportType == ObjExportType.Collision && !_hasCollisionModel)
            {
                return;
            }

            base.WriteAssetToFile(fileName);
        }

        public override void ClearExportData()
        {
            base.ClearExportData();
            _activeMaterial = null;
            _usedVertices = 0;
            _baseVertex = 0;
            _isFirstMesh = true;
        }
    }
}