using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GlmSharp;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// Contains logic for loading and extracting data from a WLD file
    /// </summary>
    public class WldFile
    {
        /// <summary>
        /// The link between fragment types and fragment classes
        /// </summary>
        private Dictionary<int, Func<WldFragment>> _fragmentBuilder;

        /// <summary>
        /// A link of indices to fragments
        /// </summary>
        private Dictionary<int, WldFragment> _fragments;

        /// <summary>
        /// The string has containing the index in the hash and the decoded string that is there
        /// </summary>
        private Dictionary<int, string> _stringHash;

        /// <summary>
        /// A collection of fragment lists that can be referenced by a fragment type
        /// </summary>
        private Dictionary<int, List<WldFragment>> _fragmentTypeDictionary;

        /// <summary>
        /// A collection of fragment lists that can be referenced by a fragment type
        /// </summary>
        private Dictionary<string, WldFragment> _fragmentNameDictionary;

        /// <summary>
        /// The shortname of the zone this WLD is from
        /// </summary>
        private readonly string _zoneName;

        /// <summary>
        /// The logger to use to output WLD information
        /// </summary>
        private readonly ILogger _logger = null;

        /// <summary>
        /// The type of WLD file this is
        /// </summary>
        private readonly WldType _wldType;

        /// <summary>
        /// The WLD file found in the PFS archive
        /// </summary>
        private readonly PfsFile _wldFile;

        /// <summary>
        /// Cached settings
        /// </summary>
        private readonly Settings _settings;

        private Dictionary<string, Material> GlobalCharacterMaterials;

        /// <summary>
        /// Constructor setting data references used during the initialization process
        /// </summary>
        /// <param name="wldFile">The WLD file bytes contained in the PFS file</param>
        /// <param name="zoneName">The shortname of the zone</param>
        /// <param name="type">The type of WLD - used to determine what to extract</param>
        /// <param name="logger">The logger used for debug output</param>
        public WldFile(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings)
        {
            _zoneName = zoneName.ToLower();
            _wldType = type;
            _wldFile = wldFile;
            _logger = logger;
            _settings = settings;
        }

        /// <summary>
        /// Initializes and instantiates the WLD file
        /// </summary>
        public bool Initialize()
        {
            _logger.LogInfo("Extracting WLD archive: " + _wldFile.Name);
            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD type: " + _wldType);

            InstantiateFragmentBuilder();

            _fragments = new Dictionary<int, WldFragment>();

            _fragmentTypeDictionary = new Dictionary<int, List<WldFragment>>();

            _fragmentNameDictionary = new Dictionary<string, WldFragment>();

            var reader = new BinaryReader(new MemoryStream(_wldFile.Bytes));

            int identifier = reader.ReadInt32();

            if (identifier != 0x54503D02)
            {
                _logger.LogError("Not a valid WLD file!");
                return false;
            }

            int version = reader.ReadInt32();

            switch (version)
            {
                case 0x00015500:
                    break;
                case 0x1000C800:
                    _logger.LogError("New WLD format not supported.");
                    return false;
                default:
                    _logger.LogError("Unrecognized WLD format.");
                    return false;
            }

            uint fragmentCount = reader.ReadUInt32();

            uint bspRegionCount = reader.ReadUInt32();

            // Should contain 0x000680D4
            int unknown = reader.ReadInt32();

            uint stringHashSize = reader.ReadUInt32();

            int unknown2 = reader.ReadInt32();

            byte[] stringHash = reader.ReadBytes((int) stringHashSize);

            ParseStringHash(WldStringDecoder.DecodeString(stringHash));

            for (int i = 0; i < fragmentCount; ++i)
            {
                uint fragSize = reader.ReadUInt32();
                int fragId = reader.ReadInt32();

                WldFragment newFrag = null;

                // Create the fragments
                newFrag = !_fragmentBuilder.ContainsKey(fragId) ? new Generic() : _fragmentBuilder[fragId]();

                newFrag.Initialize(i, fragId, (int) fragSize, reader.ReadBytes((int) fragSize), _fragments, _stringHash,
                    _logger);
                newFrag.OutputInfo(_logger);

                _fragments[i] = newFrag;

                if (!_fragmentTypeDictionary.ContainsKey(fragId))
                {
                    _fragmentTypeDictionary[fragId] = new List<WldFragment>();
                }

                if (!string.IsNullOrEmpty(newFrag.Name) && !_fragmentNameDictionary.ContainsKey(newFrag.Name))
                {
                    _fragmentNameDictionary[newFrag.Name] = newFrag;
                }

                _fragmentTypeDictionary[fragId].Add(newFrag);
            }

            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD extraction complete");

            // Character archives require a bit of "post processing" after they are instantiated
            if (_wldType == WldType.Characters)
            {
                ProcessCharacterSkins();
            }

            return true;
        }

        /// <summary>
        /// Instantiates the link between fragment hex values and fragment classes
        /// </summary>
        private void InstantiateFragmentBuilder()
        {
            _fragmentBuilder = new Dictionary<int, Func<WldFragment>>
            {
                {0x35, () => new FirstFragment()},

                // Materials
                {0x03, () => new BitmapName()},
                {0x04, () => new TextureInfo()},
                {0x05, () => new TextureInfoReference()},
                {0x30, () => new Material()},
                {0x31, () => new MaterialList()},

                // BSP Tree
                {0x21, () => new BspTree()},
                {0x22, () => new BspRegion()},
                {0x29, () => new RegionFlag()},

                // Meshes
                {0x36, () => new Mesh()},
                {0x37, () => new MeshAnimatedVertices()},
                {0x2D, () => new MeshReference()},

                // Animation
                {0x14, () => new ModelReference()},
                {0x10, () => new SkeletonTrack()},
                {0x11, () => new SkeletonTrackReference()},
                {0x12, () => new SkeletonPiece()},
                {0x13, () => new SkeletonPieceTrackReference()},

                // Lights
                {0x1B, () => new LightSource()},
                {0x1C, () => new LightSourceReference()},
                {0x28, () => new LightInfo()},
                {0x2A, () => new AmbientLight()},

                // Vertex colors
                {0x32, () => new VertexColor()},
                {0x33, () => new VertexColorReference()},

                // General
                {0x15, () => new ObjectLocation()},

                // Unused
                {0x08, () => new Camera()},
                {0x09, () => new CameraReference()},
                {0x16, () => new ZoneUnknown()},
            };
        }

        /// <summary>
        /// Parses the WLD string hash into a dictionary for easy character index access
        /// </summary>
        /// <param name="decodedHash">The decoded has to parse</param>
        private void ParseStringHash(string decodedHash)
        {
            _stringHash = new Dictionary<int, string>();

            int index = 0;

            string[] splitHash = decodedHash.Split('\0');

            foreach (var hashString in splitHash)
            {
                _stringHash[index] = hashString;

                // Advance the position by the length + the null terminator
                index += hashString.Length + 1;
            }
        }

        /// <summary>
        /// Returns a mapping of the material name to the shader type
        /// Used in exporting the bitmaps from the PFS archive
        /// </summary>
        /// <returns>Dictionary with material to shader mapping</returns>
        public Dictionary<string, List<ShaderType>> GetMaterialTypes()
        {
            var materialTypes = new Dictionary<string, List<ShaderType>>();

            for (int i = 0; i < _fragmentTypeDictionary[0x31].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x31][i] is MaterialList materialList))
                {
                    continue;
                }

                ProcessMaterialList(ref materialTypes, materialList.Materials);
            }

            return materialTypes;
        }

        private void ProcessMaterialList(ref Dictionary<string, List<ShaderType>> materialTypes,
            List<Material> materialList)
        {
            foreach (Material material in materialList)
            {
                if (material.IsInvisible)
                    continue;

                List<string> bitmapNames = material.GetAllBitmapNames();

                ShaderType shaderType = material.ShaderType;

                foreach (var bitmapName in bitmapNames)
                {
                    if (!materialTypes.ContainsKey(bitmapName))
                    {
                        materialTypes[bitmapName] = new List<ShaderType>();
                    }

                    materialTypes[bitmapName].Add(shaderType);
                }
            }
        }

        /// <summary>
        /// Processes each material list to create the texture slots
        /// Adds the alternate skin materials to their own skin dictionary
        /// </summary>
        private void ProcessCharacterSkins()
        {
            List<KeyValuePair<int, MaterialList>> materialListMapping = new List<KeyValuePair<int, MaterialList>>();
            
            foreach (WldFragment materialListFragment in _fragmentTypeDictionary[0x31])
            {
                if (!(materialListFragment is MaterialList materialList))
                {
                    continue;
                }

                materialList.CreateIndexSlotMapping(_logger);
                materialListMapping.Add(new KeyValuePair<int, MaterialList>(materialList.Index, materialList));
            }

            foreach (WldFragment materialFragment in _fragmentTypeDictionary[0x30])
            {
                if (!(materialFragment is Material material))
                {
                    continue;
                }

                if (material.IsHandled)
                {
                    continue;
                }

                if (material.GetMaterialType() == Material.CharacterMaterialType.GlobalSkin)
                {
                    if (GlobalCharacterMaterials == null)
                    {
                        GlobalCharacterMaterials = new Dictionary<string, Material>();
                    }

                    if (!GlobalCharacterMaterials.ContainsKey(material.Name))
                    {
                        GlobalCharacterMaterials.Add(material.Name, material);
                    }

                    continue;
                }

                MaterialList parentList = materialListMapping.First().Value;
                
                foreach (var listMapping in materialListMapping)
                {
                    if (material.Index < listMapping.Key)
                    {
                        break;
                    }
                    
                    parentList = listMapping.Value;
                }
                
                if (parentList == null)
                {
                    Console.WriteLine("Can't find parent material list: "+ material.Name);
                    continue;
                }

                _logger.LogError("Match: " + material.Name + " to: " + parentList.Name);
                
                parentList.AddMaterialToSkins(material, _logger);
            }
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        public void OutputFiles()
        {
            if (_wldType == WldType.Zone)
            {
                ExportZoneMeshes();
                ExportMaterialList();
            }
            else if (_wldType == WldType.Objects)
            {
                ExportZoneObjectMeshes();
                ExportMaterialList();
            }
            else if (_wldType == WldType.ZoneObjects)
            {
                ExportObjectInstanceList();
            }
            else if (_wldType == WldType.Lights)
            {
                ExportLightInstanceList();
            }
            else if (_wldType == WldType.Characters)
            {
                ExportCharacterMeshes();
                ExportMaterialList();
                CheckAllMaterials();
            }
        }

        /// <summary>
        /// Exports the zone meshes to an .obj file
        /// This includes the textures mesh, the collision mesh, and the water and lava meshes (if they exist)
        /// </summary>
        private void ExportZoneMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x36))
                return;
            
            string zoneExportFolder = _zoneName + "/" + LanternStrings.ExportZoneFolder;
            Directory.CreateDirectory(zoneExportFolder);

            bool useMeshGroups = _settings.ExportZoneMeshGroups;

            // Get all valid meshes
            var zoneMeshes = new List<Mesh>();
            bool shouldExportCollisionMesh = false;
            
            // Loop through once and validate meshes
            // If all surfaces are solid, there is no reason to export a separate collision mesh
            for (int i = 0; i < _fragmentTypeDictionary[0x36].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x36][i] is Mesh zoneMesh))
                {
                    continue;
                }

                zoneMeshes.Add(zoneMesh);

                if (!shouldExportCollisionMesh && zoneMesh.ExportSeparateCollision)
                {
                    shouldExportCollisionMesh = true;
                }
            }

            // Zone mesh
            var zoneExport = new StringBuilder();
            zoneExport.AppendLine(LanternStrings.ExportHeaderTitle + "Zone Mesh");
            zoneExport.AppendLine(LanternStrings.ObjMaterialHeader + _zoneName + LanternStrings.FormatMtlExtension);

            // Collision mesh
            var collisionExport = new StringBuilder();
            collisionExport.AppendLine(LanternStrings.ExportHeaderTitle + "Collision Mesh");

            // Water mesh
            var waterExport = new StringBuilder();
            waterExport.AppendLine(LanternStrings.ExportHeaderTitle + "Water Mesh");
            waterExport.AppendLine(LanternStrings.ObjMaterialHeader + _zoneName + LanternStrings.FormatMtlExtension);

            // Lava mesh
            var lavaExport = new StringBuilder();
            lavaExport.AppendLine(LanternStrings.ExportHeaderTitle + "Lava Mesh");
            lavaExport.AppendLine(LanternStrings.ObjMaterialHeader + _zoneName + LanternStrings.FormatMtlExtension);

            // Materials file
            var materialsExport = new StringBuilder();
            materialsExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material Definitions");
            
            // Zone mesh export
            int vertexBase = 0;
            int addedVertices = 0;
            string lastUsedTexture = string.Empty;

            for (int i = 0; i < zoneMeshes.Count; ++i)
            {
                Mesh zoneMesh = zoneMeshes[i];
                
                if (useMeshGroups)
                {
                    zoneExport.AppendLine("g " + i);
                }

                List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedTexture,
                    ObjExportType.NoSpecialZones, out addedVertices, out lastUsedTexture);

                if (outputStrings == null || outputStrings.Count == 0)
                {
                    _logger.LogWarning("Mesh has no valid output: " + zoneMesh);
                    continue;
                }

                zoneExport.Append(outputStrings[0]);
                vertexBase += addedVertices;
            }

            File.WriteAllText(zoneExportFolder + _zoneName + LanternStrings.ObjFormatExtension, zoneExport.ToString());

            // Collision mesh export
            if (shouldExportCollisionMesh)
            {
                vertexBase = 0;
                lastUsedTexture = string.Empty;

                for (int i = 0; i < zoneMeshes.Count; ++i)
                {
                    Mesh zoneMesh = zoneMeshes[i];

                    if (useMeshGroups)
                    {
                        collisionExport.AppendLine("g " + i);
                    }

                    List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedTexture,
                        ObjExportType.Collision, out addedVertices, out lastUsedTexture);

                    if (outputStrings == null || outputStrings.Count == 0)
                    {
                        continue;
                    }

                    collisionExport.Append(outputStrings[0]);
                    vertexBase += addedVertices;
                }

                File.WriteAllText(zoneExportFolder + _zoneName + "_collision" + LanternStrings.ObjFormatExtension,
                    collisionExport.ToString());
            }

            // Water mesh export
            vertexBase = 0;
            lastUsedTexture = string.Empty;

            foreach (Mesh zoneMesh in zoneMeshes)
            {
                List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedTexture,
                    ObjExportType.Water, out addedVertices, out lastUsedTexture);

                if (outputStrings == null || outputStrings.Count == 0)
                {
                    continue;
                }

                waterExport.Append(outputStrings[0]);
                vertexBase += addedVertices;
            }

            if (vertexBase != 0)
            {
                File.WriteAllText(zoneExportFolder + _zoneName + "_water" + LanternStrings.ObjFormatExtension,
                    waterExport.ToString());
            }

            // Lava mesh export
            vertexBase = 0;
            lastUsedTexture = string.Empty;

            foreach (Mesh zoneMesh in zoneMeshes)
            {
                List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedTexture,
                    ObjExportType.Lava, out addedVertices, out lastUsedTexture);

                if (outputStrings == null || outputStrings.Count == 0)
                {
                    continue;
                }

                lavaExport.Append(outputStrings[0]);
                vertexBase += addedVertices;
            }

            if (vertexBase != 0)
            {
                File.WriteAllText(zoneExportFolder + _zoneName + "_lava" + LanternStrings.ObjFormatExtension,
                    lavaExport.ToString());
            }

            // Theoretically, there should only be one texture list here
            for (int i = 0; i < _fragmentTypeDictionary[0x31].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x31][i] is MaterialList materialList))
                {
                    continue;
                }

                materialsExport.Append(materialList.GetMaterialListExport());
                break;
            }

            File.WriteAllText(zoneExportFolder + _zoneName + LanternStrings.FormatMtlExtension,
                materialsExport.ToString());
        }

        /// <summary>
        /// Export zone object meshes to .obj files and collision meshes if there are non-solid polygons
        /// Additionally, it exports a list of vertex animated instances
        /// </summary>
        private void ExportZoneObjectMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x36))
                return;

            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
            Directory.CreateDirectory(objectsExportFolder);

            // The information about models that use vertex animation
            var animatedMeshInfo = new StringBuilder();

            for (int i = 0; i < _fragmentTypeDictionary[0x36].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x36][i] is Mesh objectMesh))
                {
                    continue;
                }
                                
                string fixedObjectName = objectMesh.Name.Split('_')[0].ToLower();

                // These values are not used
                int addedVertices = 0;
                string lastUsedTexture = string.Empty;
                
                List<string> meshStrings = objectMesh.GetMeshExport(0, string.Empty,
                    ObjExportType.NoSpecialZones, out addedVertices, out lastUsedTexture);

                if (meshStrings == null || meshStrings.Count == 0)
                {
                    continue;
                }

                var objectExport = new StringBuilder();
                
                // If there are more than one outputs, it's an additional frame for an animated mesh
                for (int j = 0; j < meshStrings.Count; ++j)
                {
                    objectExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Mesh - " + fixedObjectName);
                    objectExport.AppendLine(LanternStrings.ObjMaterialHeader + fixedObjectName +
                                            LanternStrings.FormatMtlExtension);

                    // Most of the time, there will only be one
                    if (j == 0)
                    {
                        objectExport.Append(meshStrings[0]);
                        File.WriteAllText(objectsExportFolder + fixedObjectName + LanternStrings.ObjFormatExtension,
                            objectExport.ToString());

                        if (meshStrings.Count != 1)
                        {
                            animatedMeshInfo.Append(fixedObjectName);
                            animatedMeshInfo.Append(",");
                            animatedMeshInfo.Append(meshStrings.Count);
                            animatedMeshInfo.Append(",");
                            animatedMeshInfo.Append(objectMesh.AnimatedVertices.Delay);
                            animatedMeshInfo.AppendLine();
                        }

                        continue;
                    }

                    objectExport.Append(meshStrings[j - 1]);
                    File.WriteAllText(
                        objectsExportFolder + fixedObjectName + "_frame" + j + LanternStrings.ObjFormatExtension,
                        objectExport.ToString());

                    objectExport.Clear();
                }

                // Write animated mesh vertex entries (if they exist)
                if (animatedMeshInfo.Length != 0)
                {
                    var animatedMeshesHeader = new StringBuilder();
                    animatedMeshesHeader.AppendLine(LanternStrings.ExportHeaderTitle + "Animated Vertex Meshes");
                    animatedMeshesHeader.AppendLine(
                        LanternStrings.ExportHeaderFormat + "ModelName, Frames, Frame Delay");

                    File.WriteAllText(objectsExportFolder + _zoneName + "_animated_meshes.txt",
                        animatedMeshesHeader + animatedMeshInfo.ToString());
                }

                // Collision mesh
                if (objectMesh.ExportSeparateCollision)
                {
                    var collisionExport = new StringBuilder();
                    collisionExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Collision Mesh - " + fixedObjectName);

                    addedVertices = 0;
                    lastUsedTexture = string.Empty;
                    meshStrings = objectMesh.GetMeshExport(0, string.Empty, ObjExportType.Collision, out addedVertices, out lastUsedTexture);

                    if (meshStrings == null || meshStrings.Count == 0)
                    {
                        continue;
                    } 

                    File.WriteAllText(objectsExportFolder + fixedObjectName + "_collision" + LanternStrings.ObjFormatExtension, meshStrings[0]);
                }

                // Materials
                var materialsExport = new StringBuilder();
                materialsExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material Definitions");
                materialsExport.Append(objectMesh.MaterialList.GetMaterialListExport());

                File.WriteAllText(objectsExportFolder + fixedObjectName + LanternStrings.FormatMtlExtension,
                    materialsExport.ToString());
            }
        }

        /// <summary>
        /// Exports the list of objects instances
        /// This includes information about position, rotation, and scaling
        /// </summary>
        private void ExportObjectInstanceList()
        {
            string zoneExportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
            
            Directory.CreateDirectory(zoneExportFolder);
            
            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};
            
            var objectListExport = new StringBuilder();

            objectListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Instances");
            objectListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                        "ModelName, PosX, PosY, PosZ, RotX, RotY, RotZ, ScaleX, ScaleY, ScaleZ");

            for (int i = 0; i < _fragmentTypeDictionary[0x15].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x15][i] is ObjectLocation objectLocation))
                {
                    continue;
                }

                objectListExport.Append(objectLocation.ObjectName);
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Position.x.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Position.z.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Position.y.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Rotation.x.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append((objectLocation.Rotation.z).ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Rotation.y.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Scale.x.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Scale.y.ToString(format));
                objectListExport.Append(",");
                objectListExport.Append(objectLocation.Scale.z.ToString(format));
                objectListExport.AppendLine();
            }
            
            File.WriteAllText(zoneExportFolder + _zoneName + "_objects.txt", objectListExport.ToString());
        }

        /// <summary>
        /// Exports the list of light instances (contains position, colors, radius)
        /// </summary>
        private void ExportLightInstanceList()
        {
            string zoneExportFolder = _zoneName + "/" + LanternStrings.ExportZoneFolder;

            Directory.CreateDirectory(zoneExportFolder);
            
            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};

            var lightListExport = new StringBuilder();

            lightListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Light Instances");
            lightListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                       "PosX, PosY, PosZ, Radius, ColorR, ColorG, ColorB");

            for (int i = 0; i < _fragmentTypeDictionary[0x28].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x28][i] is LightInfo lightInfo))
                {
                    continue;
                }

                lightListExport.Append(lightInfo.Position.x.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.Position.z.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.Position.y.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.Radius.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.LightReference.LightSource.Color.r.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.LightReference.LightSource.Color.g.ToString(format));
                lightListExport.Append(",");
                lightListExport.Append(lightInfo.LightReference.LightSource.Color.b.ToString(format));
                lightListExport.AppendLine();
            }

            File.WriteAllText(zoneExportFolder + _zoneName + "_lights.txt", lightListExport.ToString());
        }

        /// <summary>
        /// Exports the list of material and their associated shader types
        /// This is not the same as the material definition files associated with each model
        /// </summary>
        private void ExportMaterialList()
        {
            var materialListExport = new StringBuilder();

            materialListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material List Information");
            materialListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                          "BitmapName, BitmapCount, FrameDelay (optional)");

            for (int i = 0; i < _fragmentTypeDictionary[0x31].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x31][i] is MaterialList materialList))
                {
                    continue;
                }

                foreach (Material material in materialList.Materials)
                {
                    if (material.IsInvisible)
                        continue;

                    string textureName = material.TextureInfoReference.TextureInfo.BitmapNames[0]
                        .Filename;
                    textureName = ImageWriter.GetExportedImageName(textureName, material.ShaderType);
                    textureName = textureName.Substring(0, textureName.Length - 4);
                    materialListExport.Append(textureName);
                    materialListExport.Append(",");
                    materialListExport.Append(material.TextureInfoReference.TextureInfo.BitmapNames.Count);

                    if (material.TextureInfoReference.TextureInfo.IsAnimated)
                    {
                        materialListExport.Append("," + material.TextureInfoReference.TextureInfo.AnimationDelayMs);
                    }

                    materialListExport.AppendLine();
                }
            }

            string fileName;

            if (_wldType == WldType.Zone)
            {
                string exportFolder = _zoneName + "/" + LanternStrings.ExportZoneFolder;
                Directory.CreateDirectory(exportFolder);
                fileName = exportFolder + "/" + _zoneName;
            }
            else if (_wldType == WldType.Objects)
            {
                string exportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
                Directory.CreateDirectory(exportFolder);
                fileName = exportFolder + "/" + _zoneName;
                fileName += "_objects";
            }
            else
            {
                string exportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder;
                Directory.CreateDirectory(exportFolder);
                fileName = exportFolder + "/" + _zoneName;
                fileName += "_characters";
            }

            fileName += "_materials.txt";

            File.WriteAllText(fileName, materialListExport.ToString());
        }
        
        /// <summary>
        /// Exports all meshes in the archive. If the mesh belongs to a model, it exports additional information
        /// </summary>
        private void ExportCharacterMeshes()
        {
            foreach (WldFragment meshFragment in _fragmentTypeDictionary[0x36])
            {
                if (!(meshFragment is Mesh mesh))
                {
                    continue;
                }

                // Find the model reference
                ModelReference actorReference = null;

                bool isMainModel = FindModelReference(mesh.Name.Split('_')[0] + "_ACTORDEF", out actorReference);

                if (!isMainModel && !FindModelReference(mesh.Name.Substring(0, 3) + "_ACTORDEF", out actorReference))
                {
                   // _logger.LogError("Cannot export character model: " + mesh.Name);
                    continue;
                }
                
                // If this is a skeletal model, shift the values to get the default pose (things like boats have a skeleton but no references)
                if (actorReference.SkeletonReferences.Count != 0)
                {
                    SkeletonTrack skeleton = actorReference.SkeletonReferences[0].SkeletonTrack;

                    if (skeleton == null)
                    {
                        continue;
                    }

                    mesh.ShiftSkeletonValues(skeleton, skeleton.Skeleton[0], vec3.Zero, 0);
                }

                ExportCharacterMesh(mesh, isMainModel);
            }
        }

        /// <summary>
        /// Finds a model reference (0x14) with the given name
        /// </summary>
        /// <param name="modelName">The model actor name</param>
        /// <param name="modelReference">The out reference parameter</param>
        /// <returns>Whether or not we have found the model</returns>
        private bool FindModelReference(string modelName, out ModelReference modelReference)
        {
            if (!_fragmentNameDictionary.ContainsKey(modelName))
            {
                modelReference = null;
                return false;
            }

            modelReference = _fragmentNameDictionary[modelName] as ModelReference;
            return true;
        }

        /// <summary>
        /// Exports a specific character mesh
        /// </summary>
        /// <param name="mesh">The mesh to export</param>
        /// <param name="isMainModel"></param>
        private void ExportCharacterMesh(Mesh mesh, bool isMainModel)
        {
            string exportDirectory = _zoneName + "/" + LanternStrings.ExportCharactersFolder;
            Directory.CreateDirectory(exportDirectory);

            if (mesh.Name.ToLower().StartsWith("baf"))
            {
                
            }

            if (isMainModel)
            {
                List<string> materialExports = mesh.MaterialList.GetMaterialSkinExports();
                
                for (var i = 0; i < materialExports.Count; i++)
                {
                    var materialFile = materialExports[i];
                    string fileName = Mesh.FixCharacterMeshName(mesh.Name, true) + (i > 0 ? i.ToString() : "") +
                                      LanternStrings.FormatMtlExtension;
                    
                    File.WriteAllText(exportDirectory + fileName, materialFile);
                }
            }

            File.WriteAllText(exportDirectory + Mesh.FixCharacterMeshName(mesh.Name, isMainModel) + LanternStrings.ObjFormatExtension,
                mesh.GetSkeletonMeshExport(isMainModel ? string.Empty : mesh.ParseMeshNameDetails()));
        }

        private void CheckAllMaterials()
        {
            foreach (var material in _fragmentTypeDictionary[0x30])
            {
                Material mat = material as Material;

                if (mat == null)
                {
                    continue;
                }
                
                //_logger.LogError("Material: " + mat.Name + " is a slot reference: " + mat.GetMaterialType());
            }
        }
    }
}