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
        public string ZoneShortname => _zoneName;

        public WldType WldType => _wldType;

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
            var writer = new BinaryWriter(new MemoryStream(_wldFile.Bytes));

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

            long readPosition = 0;
            
            for (int i = 0; i < fragmentCount; ++i)
            {
                uint fragSize = reader.ReadUInt32();
                FragmentType fragId = (FragmentType)reader.ReadInt32();

                // Create the fragments
                var newFragment = !_fragmentBuilder.ContainsKey(fragId) ? new Generic() : _fragmentBuilder[fragId]();

                if (newFragment is Generic)
                {
                    _logger.LogWarning($"WldFile: Unhandled fragment type: {fragId:x}");
                }

                readPosition = reader.BaseStream.Position;

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

            if (_wldType == WldType.Objects)
            {
                var fileStream = File.Create("objects.wld");
                writer.BaseStream.Seek(0, SeekOrigin.Begin);
                writer.BaseStream.CopyTo(fileStream);
                fileStream.Close();
            }
            
            return true;
        }

        public List<WldFragment> GetFragmentsOfType(FragmentType type)
        {
            if (!_fragmentTypeDictionary.ContainsKey(type))
            {
                return null;
            }

            return _fragmentTypeDictionary[type];
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
                // Materials
                {FragmentType.Bitmap, () => new BitmapName()},
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
                {FragmentType.ModelReference, () => new Actor()},
                {FragmentType.SkeletonHierarchy, () => new SkeletonHierarchy()},
                {FragmentType.HierSpriteFragment, () => new SkeletonHierarchyReference()},
                {FragmentType.TrackDefFragment, () => new TrackDefFragment()},
                {FragmentType.TrackFragment, () => new TrackFragment()},

                // Lights
                {FragmentType.Light, () => new LightSource()},
                {FragmentType.LightReference, () => new LightSourceReference()},
                {FragmentType.LightInstance, () => new LightInstance()},
                {FragmentType.AmbientLight, () => new AmbientLight()},
                {FragmentType.GlobalAmbientLightColor, () => new GlobalAmbientLightColor()},

                // Vertex colors
                {FragmentType.VertexColor, () => new VertexColors()},
                {FragmentType.VertexColorReference, () => new VertexColorReference()},

                // General
                {FragmentType.ObjectInstance, () => new ObjectInstance()},

                // Not used/unknown
                {FragmentType.Camera, () => new Camera()},
                {FragmentType.CameraReference, () => new CameraReference()},
                {FragmentType.Fragment16, () => new Fragment16()},
                {FragmentType.Fragment17, () => new Fragment17()},
                {FragmentType.Fragment18, () => new Fragment18()},
                {FragmentType.Fragment2F, () => new MeshAnimatedVerticesReference()},
            };
        }
        
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
        public List<string> GetMaskedTextures()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.MaterialList))
            {
                _logger.LogWarning("Cannot get material types. No texture list found.");
                return null;
            }

            List<string> maskedTextures = new List<string>();

            foreach (WldFragment materialListFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                MaterialList materialList = materialListFragment as MaterialList;

                if (materialList == null)
                {
                    continue;
                }

                foreach (var material in materialList.Materials)
                {
                    if (material.ShaderType != ShaderType.TransparentMasked)
                    {
                        continue;
                    }
                    
                    maskedTextures.AddRange(material.GetAllBitmapNames(true));
                }
            }
          
            return maskedTextures;
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected virtual void ExportData()
        {
            ExportActors();
            ExportMaterialList();
            ExportMeshes();
            ExportSkeletonAndAnimations();
        }

        /// <summary>
        /// Exports the list of all textures
        /// This is not the same as the material definition files associated with each model
        /// </summary>
        private void ExportMaterialList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.MaterialList))
            {
                _logger.LogWarning("Cannot export texture lists. No materials found.");
                return;
            }

            MaterialListWriter export = new MaterialListWriter();

            foreach (WldFragment listFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                export.AddFragmentData(listFragment);
            }

            string exportFilename = _zoneName + "/materials";

            if (_wldType == WldType.Zone)
            {
                exportFilename += "_zone";
            }
            else if (_wldType == WldType.Objects)
            {
                exportFilename += "_objects";
            }
            else if (_wldType == WldType.Characters)
            {
                exportFilename += "_characters";
            }
            else if (_wldType == WldType.Sky)
            {
                exportFilename += "_sky";
            }

            exportFilename += ".txt";

            export.WriteAssetToFile(exportFilename);
        }
        
        private void ExportMeshes()
        {
            MeshExporter.ExportMeshes(this, _settings);
        }

        protected void ExportMeshList()
        {
            int objectCount = _fragmentTypeDictionary[FragmentType.Mesh].Count;
            ObjectListWriter objectWriter = new ObjectListWriter(objectCount);
            
            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                objectWriter.AddFragmentData(fragment);
            }
            
            objectWriter.WriteAssetToFile(GetRootExportFolder() + "meshes_" + _wldType.ToString().ToLower() + ".txt");
        }

        public string GetExportFolderForWldType()
        {
            switch (_wldType)
            {
                case WldType.Zone:
                    return _zoneName + "/Zone/";
                case WldType.Models:
                case WldType.ZoneObjects:
                case WldType.Lights:
                    return GetRootExportFolder();
                case WldType.Objects:
                    return GetRootExportFolder() + "Objects/";
                case WldType.Sky:
                    return "sky/";
                case WldType.Characters:
                    return GetRootExportFolder() + "Characters/";
                default:
                    return string.Empty;
            }
        }

        protected string GetRootExportFolder()
        {
            return _zoneName + "/";
        }

        private void ExportActors()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.ModelReference))
            {
                return;
            }
            
            ActorWriter actorWriterStatic = new ActorWriter(false);
            ActorWriter actorWriterAnimated = new ActorWriter(true);

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.ModelReference])
            {
                actorWriterStatic.AddFragmentData(actorFragment);
                actorWriterAnimated.AddFragmentData(actorFragment);
            }
            
            string exportPath = GetRootExportFolder();
            exportPath += "actors_" + _wldType.ToString().ToLower();
            
            actorWriterStatic.WriteAssetToFile(exportPath + "_static.txt");
            actorWriterAnimated.WriteAssetToFile(exportPath + "_animated.txt");
        }
        
        protected void ExportSkeletonAndAnimations()
        {
            string skeletonsFolder = GetExportFolderForWldType() + "Skeletons/";
            string animationsFolder = GetExportFolderForWldType() + "Animations/";
            
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.SkeletonHierarchy))
            {
                _logger.LogWarning("Cannot export animations. No model references.");
                return;
            }

            SkeletonHierarchyWriter skeletonWriter = new SkeletonHierarchyWriter();
            AnimationWriter animationWriter = new AnimationWriter(_wldType == WldType.Characters);
            
            foreach (var skeletonFragment in _fragmentTypeDictionary[FragmentType.SkeletonHierarchy])
            {
                SkeletonHierarchy skeleton = skeletonFragment as SkeletonHierarchy;

                if (skeleton == null)
                {
                    continue;
                }
                
                skeletonWriter.AddFragmentData(skeleton);
                skeletonWriter.WriteAssetToFile(skeletonsFolder + skeleton.ModelBase + ".txt");
                skeletonWriter.ClearExportData();

                foreach (var animation in skeleton.Animations)
                {
                    var modelBase = string.IsNullOrEmpty(animation.Value.AnimModelBase)
                        ? skeleton.ModelBase
                        : animation.Value.AnimModelBase;
                    animationWriter.SetTargetAnimation(animation.Key);
                    animationWriter.AddFragmentData(skeleton);
                    string fileName = modelBase + "_" + animation.Key + ".txt";
                    animationWriter.WriteAssetToFile(animationsFolder + fileName);
                    animationWriter.ClearExportData();
                }
            }
        }
    }
}