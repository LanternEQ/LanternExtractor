using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// Contains shared logic for loading and extracting data from a WLD file
    /// </summary>
    public abstract class WldFile
    {
        /// <summary>
        /// The link between fragment types and fragment classes
        /// </summary>
        private Dictionary<FragmentType, Func<WldFragment>> _fragmentBuilder;

        /// <summary>
        /// A link of indices to fragments
        /// </summary>
        protected List<WldFragment> _fragments;

        /// <summary>
        /// The string has containing the index in the hash and the decoded string that is there
        /// </summary>
        private Dictionary<int, string> _stringHash;

        /// <summary>
        /// A collection of fragment lists that can be referenced by a fragment type
        /// </summary>
        protected Dictionary<FragmentType, List<WldFragment>> _fragmentTypeDictionary;

        /// <summary>
        /// A collection of fragment lists that can be referenced by a fragment type
        /// </summary>
        protected Dictionary<string, WldFragment> _fragmentNameDictionary;

        protected List<BspRegion> _bspRegions;

        /// <summary>
        /// The shortname of the zone this WLD is from
        /// </summary>
        protected readonly string _zoneName;

        /// <summary>
        /// The logger to use to output WLD information
        /// </summary>
        protected readonly ILogger _logger;

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
        
        private const int WldFileIdentifier = 0x54503D02;
        
        private const int WldFormatOld = 0x00015500;
        private const int WldFormatNew = 0x1000C800;

        /// <summary>
        /// Is this the new WLD format? Some data types are different
        /// </summary>
        private bool _isNewWldFormat;

        protected readonly WldFile _wldToInject;

        /// <summary>
        /// Constructor setting data references used during the initialization process
        /// </summary>
        /// <param name="wldFile">The WLD file bytes contained in the PFS file</param>
        /// <param name="zoneName">The shortname of the zone</param>
        /// <param name="type">The type of WLD - used to determine what to extract</param>
        /// <param name="logger">The logger used for debug output</param>
        protected WldFile(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings, WldFile fileToInject)
        {
            _wldFile = wldFile;
            _zoneName = zoneName.ToLower();
            _wldType = type;
            _logger = logger;
            _settings = settings;
            _wldToInject = fileToInject;
        }

        /// <summary>
        /// Initializes and instantiates the WLD file
        /// </summary>
        public virtual bool Initialize(bool exportData = true)
        {
            _logger.LogInfo("Extracting WLD archive: " + _wldFile.Name);
            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD type: " + _wldType);

            InstantiateFragmentBuilder();

            _fragments = new List<WldFragment>();
            _fragmentTypeDictionary = new Dictionary<FragmentType, List<WldFragment>>();
            _fragmentNameDictionary = new Dictionary<string, WldFragment>();
            _bspRegions = new List<BspRegion>();

            var reader = new BinaryReader(new MemoryStream(_wldFile.Bytes));

            int identifier = reader.ReadInt32();

            if (identifier != WldFileIdentifier)
            {
                _logger.LogError("Not a valid WLD file!");
                return false;
            }

            int version = reader.ReadInt32();

            switch (version)
            {
                case WldFormatOld:
                    break;
                case WldFormatNew:
                    _isNewWldFormat = true;
                    _logger.LogWarning("New WLD format not fully supported.");
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
                FragmentType fragId = (FragmentType)reader.ReadInt32();

                WldFragment newFragment;

                // Create the fragments
                newFragment = !_fragmentBuilder.ContainsKey(fragId) ? new Generic() : _fragmentBuilder[fragId]();

                if (newFragment is Generic)
                {
                    _logger.LogWarning($"Unhandled fragment type: {fragId:x}");
                }

                newFragment.Initialize(i, fragId, (int) fragSize, reader.ReadBytes((int) fragSize), _fragments, _stringHash,
                    _isNewWldFormat,
                    _logger);
                newFragment.OutputInfo(_logger);

                _fragments.Add(newFragment);

                if (!_fragmentTypeDictionary.ContainsKey(fragId))
                {
                    _fragmentTypeDictionary[fragId] = new List<WldFragment>();
                }

                if (!string.IsNullOrEmpty(newFragment.Name) && !_fragmentNameDictionary.ContainsKey(newFragment.Name))
                {
                    _fragmentNameDictionary[newFragment.Name] = newFragment;
                }

                if (fragId == FragmentType.BspRegion)
                {
                    _bspRegions.Add(newFragment as BspRegion);
                }

                _fragmentTypeDictionary[fragId].Add(newFragment);
            }

            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD extraction complete");

            ProcessData();

            if (exportData)
            {
                ExportData();
            }

            return true;
        }

        protected virtual void ProcessData()
        {
            
        }

        /// <summary>
        /// Instantiates the link between fragment hex values and fragment classes
        /// </summary>
        private void InstantiateFragmentBuilder()
        {
            _fragmentBuilder = new Dictionary<FragmentType, Func<WldFragment>>
            {
                {FragmentType.FirstFragment, () => new FirstFragment()},

                // Materials
                {FragmentType.Bitmap, () => new Bitmap()},
                {FragmentType.BitmapInfo, () => new BitmapInfo()},
                {FragmentType.BitmapInfoReference, () => new BitmapInfoReference()},
                {FragmentType.Material, () => new Material()},
                {FragmentType.MaterialList, () => new MaterialList()},

                // BSP Tree
                {FragmentType.BspTree, () => new BspTree()},
                {FragmentType.BspRegion, () => new BspRegion()},
                {FragmentType.BspRegionType, () => new BspRegionType()},

                // Meshes
                {FragmentType.Mesh, () => new Mesh()},
                {FragmentType.MeshVertexAnimation, () => new MeshAnimatedVertices()},
                {FragmentType.MeshReference, () => new MeshReference()},

                // Animation
                {FragmentType.ModelReference, () => new ModelReference()},
                {FragmentType.HierSpriteDefFragment, () => new HierSpriteDefFragment()},
                {FragmentType.HierSpriteFragment, () => new HierSpriteFragment()},
                {FragmentType.TrackDefFragment, () => new TrackDefFragment()},
                {FragmentType.TrackFragment, () => new TrackFragment()},

                // Lights
                {FragmentType.Light, () => new LightSource()},
                {FragmentType.LightReference, () => new LightSourceReference()},
                {FragmentType.LightInstance, () => new LightInfo()},
                {FragmentType.AmbientLight, () => new AmbientLight()},

                // Vertex colors
                {FragmentType.VertexColor, () => new VertexColor()},
                {FragmentType.VertexColorReference, () => new VertexColorReference()},

                // General
                {FragmentType.ObjectInstance, () => new ObjectInstance()},

                // Not used/unknown
                {FragmentType.Camera, () => new Camera()},
                {FragmentType.CameraReference, () => new CameraReference()},
                {FragmentType.Fragment16, () => new Fragment16()},
                {FragmentType.Fragment17, () => new Fragment17()},
                {FragmentType.Fragment18, () => new Fragment18()},
                {FragmentType.Fragment2F, () => new Fragment2F()},
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

            StringBuilder stringHashDump = new StringBuilder();
            
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
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.MaterialList))
            {
                _logger.LogWarning("Cannot get material types. No texture list found.");
                return null;
            }

            var materialTypes = new Dictionary<string, List<ShaderType>>();

            foreach (WldFragment materialListFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                MaterialList materialList= materialListFragment as MaterialList;

                if (materialList == null)
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
        protected virtual void ExportData()
        {
            ExportMaterialList();
        }

        /// <summary>
        /// Exports the list of material and their associated shader types
        /// This is not the same as the material definition files associated with each model
        /// </summary>
        protected void ExportMaterialList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.MaterialList))
            {
                _logger.LogWarning("Cannot export material lists. No lists found.");
                return;
            }

            MaterialListExporter exporter = new MaterialListExporter();

            foreach (WldFragment listFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                exporter.AddFragmentData(listFragment);
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

            exporter.WriteAssetToFile(fileName);
        }
    }
}