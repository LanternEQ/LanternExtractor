using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        
        protected Dictionary<Type, List<WldFragment>> _fragmentTypeDictionary2;

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

        protected SkeletonHierarchy _lastSkeleton;

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
            _fragmentTypeDictionary2 = new Dictionary<Type, List<WldFragment>>();
            _fragmentNameDictionary = new Dictionary<string, WldFragment>();
            _bspRegions = new List<BspRegion>();
            
            var reader = new BinaryReader(new MemoryStream(_wldFile.Bytes));
            
            byte[] writeBytes = reader.ReadBytes(_wldFile.Bytes.Length);
            reader.BaseStream.Position = 0;
            var writer = new BinaryWriter(new MemoryStream(writeBytes));

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

                if (fragId == FragmentType.SkeletonHierarchy && i == 125)
                {
                }

                newFragment.Initialize(i, fragId, (int) fragSize, reader.ReadBytes((int) fragSize), _fragments, _stringHash,
                    _isNewWldFormat,
                    _logger);
                newFragment.OutputInfo(_logger);

                _fragments.Add(newFragment);

                if (!_fragmentTypeDictionary.ContainsKey(fragId))
                {
                    _fragmentTypeDictionary[fragId] = new List<WldFragment>();
                    _fragmentTypeDictionary2[newFragment.GetType()] = new List<WldFragment>();
                }

                if (!string.IsNullOrEmpty(newFragment.Name) && !_fragmentNameDictionary.ContainsKey(newFragment.Name))
                {
                    _fragmentNameDictionary[newFragment.Name] = newFragment;
                }

                if (fragId == FragmentType.BspRegion)
                {
                    _bspRegions.Add(newFragment as BspRegion);
                }
/*
                long cachedPosition = reader.BaseStream.Position;
                // Create data mods class
                if (_wldType == WldType.Zone && newFragment.Type == FragmentType.Mesh)
                {
                    // Get vertex color count
                    int skip = 18 * 4 + 3 * 2;

                    long colorCountLocation = readPosition + skip;

                    reader.BaseStream.Position = colorCountLocation;

                    int count = reader.ReadInt16();

                    long colorsLocation = readPosition + (newFragment as Mesh).ColorStart;
                    reader.BaseStream.Position = colorsLocation;
                    writer.BaseStream.Position = colorsLocation;

                    Random random = new Random();
                    for (int j = 0; j < count; ++j)
                    {
                        var dwordValue = reader.ReadInt32();
                        var bytesValue = BitConverter.GetBytes(dwordValue);
                        byte amount = 255;//(byte)((random.Next(int.MaxValue) % 2 == 0) ? 0 : 255);
                        bytesValue[3] = amount;
                        writer.Write(bytesValue);
                    }
                }

                reader.BaseStream.Position = cachedPosition;*/

                _fragmentTypeDictionary[fragId].Add(newFragment);
                _fragmentTypeDictionary2[newFragment.GetType()].Add(newFragment);

                if (newFragment is SkeletonHierarchy fragment)
                {
                    _lastSkeleton = fragment;
                }

                if (newFragment is TrackFragment)
                {
                    if (!(newFragment as TrackFragment).IsPoseAnimation)
                    {
                        
                    }
                }
            }

            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD extraction complete");

            ProcessData();

            if (exportData)
            {
                ExportData();
            }

            if (_wldType == WldType.Zone)
            {
                File.WriteAllBytes("arena.wld", writeBytes);
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
        
        public List<T> GetFragmentsOfType2<T>() where T : WldFragment
        {
            if (!_fragmentTypeDictionary2.ContainsKey(typeof(T)))
            {
                return new List<T>();
            }

            return _fragmentTypeDictionary2[typeof(T)].Cast<T>().ToList();
        }

        protected T GetFragmentByName<T>(string fragmentName) where T : WldFragment
        {
            if (!_fragmentNameDictionary.ContainsKey(fragmentName))
            {
                return default(T);
            }

            return _fragmentNameDictionary[fragmentName] as T;
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
                {FragmentType.AlternateMesh, () => new AlternateMesh()},

                // Animation
                {FragmentType.Actor, () => new Actor()},
                {FragmentType.SkeletonHierarchy, () => new SkeletonHierarchy()},
                {FragmentType.SkeletonHierarchyReference, () => new SkeletonHierarchyReference()},
                {FragmentType.TrackDefFragment, () => new TrackDefFragment()},
                {FragmentType.TrackFragment, () => new TrackFragment()},

                // Lights
                {FragmentType.Light, () => new LightSource()},
                {FragmentType.LightReference, () => new LightSourceReference()},
                {FragmentType.LightInstance, () => new LightInstance()},
                {FragmentType.AmbientLight, () => new AmbientLight()},
                {FragmentType.AmbientLightColor, () => new AmbientLightColor()},

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
                {FragmentType.Fragment26, () => new ParticleSprite()},
                {FragmentType.Fragment27, () => new ParticleSpriteReference()},
                {FragmentType.ParticleCloud, () => new ParticleCloud()},
                {FragmentType.Fragment06, () => new Fragment06()},
                {FragmentType.Fragment07, () => new Fragment07()},
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

                if (materialList.AdditionalMaterials != null)
                {
                    foreach (var material in materialList.AdditionalMaterials)
                    {
                        if (material.ShaderType != ShaderType.TransparentMasked)
                        {
                            continue;
                        }

                        maskedTextures.AddRange(material.GetAllBitmapNames(true));
                    }
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

            TextAssetWriter export = null;
            string exportFilename = string.Empty;

            if (_wldType == WldType.Characters && _settings.ExportAllCharacterToSingleFolder)
            {
                export = new MaterialListGlobalWriter("all/materials_characters.txt");
                exportFilename = "all/materials";
            }
            else if (_wldType == WldType.Equipment)
            {
                export = new MaterialListGlobalWriter("equipment/materials_equipment.txt");
                exportFilename = "equipment/materials";
            }
            else
            {
                // TODO: Replace this with the root path
                export = new MaterialListWriter();
                exportFilename = _zoneName + "/materials";
            }

            foreach (WldFragment listFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                export.AddFragmentData(listFragment);
            }
            
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
            else if (_wldType == WldType.Equipment)
            {
                exportFilename += "_equipment";
            }

            exportFilename += ".txt";

            export.WriteAssetToFile(exportFilename);
        }
        
        private void ExportMeshes()
        {
            MeshExporter.ExportMeshes(this, _settings, _logger);
        }

        protected void ExportMeshList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                return;
            }
            
            int objectCount = _fragmentTypeDictionary[FragmentType.Mesh].Count;

            TextAssetWriter objectWriter = null;
            string exportPath = string.Empty;

            if (_wldType == WldType.Characters && _settings.ExportAllCharacterToSingleFolder)
            {
                objectWriter = new ObjectListGlobalWriter(objectCount);
                exportPath = "all/meshes_characters.txt";
            }
            else
            {
                objectWriter = new ObjectListWriter(objectCount);
                exportPath = GetRootExportFolder() + "meshes_" + _wldType.ToString().ToLower() + ".txt";
            }
            
            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                objectWriter.AddFragmentData(fragment);
            }
            
            objectWriter.WriteAssetToFile(exportPath);
        }

        public string GetExportFolderForWldType()
        {
            switch (_wldType)
            {
                case WldType.Zone:
                    return _zoneName + "/Zone/";
                case WldType.Equipment:
                    return "equipment/";
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
            if (_wldType == WldType.Equipment)
            {
                return "equipment/";
            }
            
            return _wldType == WldType.Characters && _settings.ExportAllCharacterToSingleFolder ? "all/" : _zoneName + "/";
        }

        private void ExportActors()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            TextAssetWriter actorWriterStatic, actorWriterSkeletal, actorWriterParticle, actorWriterSprite2d;
            
            if (_wldType == WldType.Equipment)
            {
                actorWriterStatic = new ActorWriterNewGlobal(ActorType.Static);
                actorWriterSkeletal = new ActorWriterNewGlobal(ActorType.Skeletal);
                actorWriterParticle = new ActorWriterNewGlobal(ActorType.Particle);
                actorWriterSprite2d = new ActorWriterNewGlobal(ActorType.Sprite);
            }
            else
            {
                actorWriterStatic = new ActorWriter(ActorType.Static);
                actorWriterSkeletal = new ActorWriter(ActorType.Skeletal);
                actorWriterParticle = new ActorWriter(ActorType.Particle);
                actorWriterSprite2d = new ActorWriter(ActorType.Sprite);
            }

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                actorWriterStatic.AddFragmentData(actorFragment);
                actorWriterSkeletal.AddFragmentData(actorFragment);
                actorWriterParticle.AddFragmentData(actorFragment);
                actorWriterSprite2d.AddFragmentData(actorFragment);
            }
            
            string exportPath = GetRootExportFolder();
            exportPath += "actors_" + _wldType.ToString().ToLower();
            
            actorWriterStatic.WriteAssetToFile(exportPath + "_static.txt");
            actorWriterSkeletal.WriteAssetToFile(exportPath + "_skeletal.txt");
            actorWriterParticle.WriteAssetToFile(exportPath + "_particle.txt");
            actorWriterSprite2d.WriteAssetToFile(exportPath + "_sprite.txt");
        }
        
        protected void ExportSkeletonAndAnimations()
        {
            string skeletonsFolder = GetExportFolderForWldType() + "Skeletons/";
            string animationsFolder = GetExportFolderForWldType() + "Animations/";

            var skeletons = GetFragmentsOfType(FragmentType.SkeletonHierarchy);

            if (skeletons == null)
            {
                if (_wldToInject == null)
                {
                    _logger.LogWarning("Cannot export animations. No model references.");
                    return;
                }

                skeletons = _wldToInject.GetFragmentsOfType(FragmentType.SkeletonHierarchy);
                
                if (skeletons == null)
                {
                    _logger.LogWarning("Cannot export animations. No model references.");
                    return;
                }
            }
            
            SkeletonHierarchyWriter skeletonWriter = new SkeletonHierarchyWriter();
            AnimationWriter animationWriter = new AnimationWriter(_wldType == WldType.Characters);
            
            foreach (var skeletonFragment in skeletons)
            {
                SkeletonHierarchy skeleton = skeletonFragment as SkeletonHierarchy;

                if (skeleton == null)
                {
                    continue;
                }

                string filePath = skeletonsFolder + skeleton.ModelBase + ".txt";
                
                skeletonWriter.AddFragmentData(skeleton);

                // TODO: Put this elsewhere - what does this even do?
                if (_wldType == WldType.Characters && _settings.ExportAllCharacterToSingleFolder)
                {
                    if (File.Exists(filePath))
                    {
                        var file = File.ReadAllText(filePath);
                        int oldFileSize = file.Length;
                        int newFileSize = skeletonWriter.GetExportByteCount();

                        if (newFileSize > oldFileSize)
                        {
                            skeletonWriter.WriteAssetToFile(filePath);
                        }
 
                        skeletonWriter.ClearExportData();
                    }
                    else
                    {
                        skeletonWriter.WriteAssetToFile(filePath);
                        skeletonWriter.ClearExportData();
                    }
                }
                else
                {
                    skeletonWriter.WriteAssetToFile(filePath);
                    skeletonWriter.ClearExportData();
                }
                


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