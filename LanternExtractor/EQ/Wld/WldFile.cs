using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// Contains logic for loading and extracting data from a WLD file
    /// </summary>
    public abstract class WldFile
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
        protected Dictionary<int, List<WldFragment>> _fragmentTypeDictionary;

        /// <summary>
        /// A collection of fragment lists that can be referenced by a fragment type
        /// </summary>
        private Dictionary<string, WldFragment> _fragmentNameDictionary;

        protected List<BspRegion> _bspRegions;

        /// <summary>
        /// The shortname of the zone this WLD is from
        /// </summary>
        protected readonly string _zoneName;

        /// <summary>
        /// The logger to use to output WLD information
        /// </summary>
        protected readonly ILogger _logger = null;

        /// <summary>
        /// The type of WLD file this is
        /// </summary>
        protected readonly WldType _wldType;

        /// <summary>
        /// The WLD file found in the PFS archive
        /// </summary>
        private readonly PfsFile _wldFile;

        /// <summary>
        /// Cached settings
        /// </summary>
        protected readonly Settings _settings;

        /// <summary>
        /// Is this the new WLD format? Some data types are different
        /// </summary>
        private bool _isNewWldFormat;

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
        public virtual bool Initialize()
        {
            _logger.LogInfo("Extracting WLD archive: " + _wldFile.Name);
            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD type: " + _wldType);

            InstantiateFragmentBuilder();

            _fragments = new Dictionary<int, WldFragment>();
            _fragmentTypeDictionary = new Dictionary<int, List<WldFragment>>();
            _fragmentNameDictionary = new Dictionary<string, WldFragment>();
            _bspRegions = new List<BspRegion>();

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
                    _isNewWldFormat = true;
                    _logger.LogWarning("New WLD format not fully spported.");
                    break;
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

                if (newFrag is Generic)
                {
                    _logger.LogWarning($"Unhandled fragment type: {fragId:x}");
                }

                newFrag.Initialize(i, fragId, (int) fragSize, reader.ReadBytes((int) fragSize), _fragments, _stringHash,
                    _isNewWldFormat,
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

                if (fragId == 0x22)
                {
                    _bspRegions.Add(newFrag as BspRegion);
                }

                _fragmentTypeDictionary[fragId].Add(newFrag);
            }

            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD extraction complete");

            ExportWldData();

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
                {0x10, () => new HierSpriteDefFragment()},
                {0x11, () => new HierSpriteFragment()},
                {0x12, () => new TrackDefFragment()},
                {0x13, () => new TrackFragment()},

                // Lights
                {0x1B, () => new LightSource()},
                {0x1C, () => new LightSourceReference()},
                {0x28, () => new LightInfo()},
                {0x2A, () => new AmbientLight()},

                // Vertex colors
                {0x32, () => new VertexColor()},
                {0x33, () => new VertexColorReference()},

                // General
                {0x15, () => new ObjectInstance()},

                // Unused
                {0x08, () => new Camera()},
                {0x09, () => new CameraReference()},
                {0x16, () => new ZoneUnknown()},
                {0x17, () => new Fragment17()},
                {0x18, () => new Fragment18()},
                {0x2F, () => new Fragment2F()},
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
            if (!_fragmentTypeDictionary.ContainsKey(0x31))
            {
                _logger.LogWarning("Cannot get material types. No texture list found.");
                return null;
            }

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
                if (material.GetFirstBitmapNameWithoutExtension() == string.Empty)
                {
                    continue;
                }

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
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected abstract void ExportWldData();

        /// <summary>
        /// Exports the list of material and their associated shader types
        /// This is not the same as the material definition files associated with each model
        /// </summary>
        private void ExportMaterialList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x31))
            {
                _logger.LogWarning("Cannot export material list. No list found.");
                return;
            }

            var materialListExport = new StringBuilder();

            materialListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material List Information");
            materialListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                          "BitmapName, BitmapCount, AnimationDelayMs (optional)");

            for (int i = 0; i < _fragmentTypeDictionary[0x31].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x31][i] is MaterialList materialList))
                {
                    continue;
                }

                foreach (Material material in materialList.Materials)
                {
                    if (material.ShaderType == ShaderType.Invisible)
                    {
                        continue;
                    }

                    string materialPrefix = MaterialList.GetMaterialPrefix(material.ShaderType);

                    string textureName = material.TextureInfoReference.TextureInfo.BitmapNames[0]
                        .Filename;

                    textureName = textureName.Substring(0, textureName.Length - 4);
                    materialListExport.Append(materialPrefix);
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

            string fileName = _zoneName + "/" + _zoneName + "_materials";

            if (_wldType == WldType.Objects)
            {
                fileName += "_objects";
            }
            else if (_wldType == WldType.Characters)
            {
                fileName += "_characters";
            }

            fileName += ".txt";

            string directory = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(directory))
            {
                // TODO: Handle error
                return;
            }

            File.WriteAllText(fileName, materialListExport.ToString());
        }
    }
}