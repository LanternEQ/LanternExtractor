using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;
using LanternExtractor.EQ;

namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// Contains shared logic for loading and extracting data from a WLD file
    /// </summary>
    public abstract class WldFile
    {
        public string RootExportFolder;
        public string ZoneShortname => _zoneName;


        public WldType WldType => _wldType;

        /// <summary>
        /// The link between fragment types and fragment classes
        /// </summary>
        private Dictionary<int, Func<WldFragment>> _fragmentBuilder;

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
        //protected Dictionary<FragmentType, List<WldFragment>> _fragmentTypeDictionary;
        protected Dictionary<Type, List<WldFragment>> _fragmentTypeDictionary;

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

        /// <summary>
        /// Is this the new WLD format? Some data types are different
        /// </summary>
        private bool _isNewWldFormat;

        protected readonly WldFile _wldToInject;


        public Dictionary<string, string> FilenameChanges = new Dictionary<string, string>();

        /// <summary>
        /// Constructor setting data references used during the initialization process
        /// </summary>
        /// <param name="wldFile">The WLD file bytes contained in the PFS file</param>
        /// <param name="zoneName">The shortname of the zone</param>
        /// <param name="type">The type of WLD - used to determine what to extract</param>
        /// <param name="logger">The logger used for debug output</param>
        protected WldFile(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile fileToInject)
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
        public virtual bool Initialize(string rootFolder, bool exportData = true)
        {
            RootExportFolder = rootFolder;
            _logger.LogInfo("Extracting WLD archive: " + _wldFile.Name);
            _logger.LogInfo("-----------------------------------");
            _logger.LogInfo("WLD type: " + _wldType);

            _fragments = new List<WldFragment>();
            _fragmentTypeDictionary = new Dictionary<Type, List<WldFragment>>();
            _fragmentNameDictionary = new Dictionary<string, WldFragment>();
            _bspRegions = new List<BspRegion>();

            var reader = new BinaryReader(new MemoryStream(_wldFile.Bytes));

            byte[] writeBytes = reader.ReadBytes(_wldFile.Bytes.Length);
            reader.BaseStream.Position = 0;
            var writer = new BinaryWriter(new MemoryStream(writeBytes));

            int identifier = reader.ReadInt32();

            if (identifier != WldIdentifier.WldFileIdentifier)
            {
                _logger.LogError("Not a valid WLD file!");
                return false;
            }

            int version = reader.ReadInt32();

            switch (version)
            {
                case WldIdentifier.WldFormatOldIdentifier:
                    break;
                case WldIdentifier.WldFormatNewIdentifier:
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

            byte[] stringHash = reader.ReadBytes((int)stringHashSize);

            ParseStringHash(WldStringDecoder.DecodeString(stringHash));

            long readPosition = 0;

            for (int i = 0; i < fragmentCount; ++i)
            {
                uint fragSize = reader.ReadUInt32();
                int fragId = reader.ReadInt32();

                var newFragment = !WldFragmentBuilder.Fragments.ContainsKey(fragId)
                    ? new Generic()
                    : WldFragmentBuilder.Fragments[fragId]();

                if (newFragment is Generic)
                {
                    _logger.LogWarning($"WldFile: Unhandled fragment type: {fragId:x}");
                }

                newFragment.Initialize(i, (int)fragSize, reader.ReadBytes((int)fragSize), _fragments, _stringHash,
                    _isNewWldFormat,
                    _logger);
                newFragment.OutputInfo(_logger);

                _fragments.Add(newFragment);

                if (!_fragmentTypeDictionary.ContainsKey(newFragment.GetType()))
                {
                    _fragmentTypeDictionary[newFragment.GetType()] = new List<WldFragment>();
                }

                if (!string.IsNullOrEmpty(newFragment.Name) && !_fragmentNameDictionary.ContainsKey(newFragment.Name))
                {
                    _fragmentNameDictionary[newFragment.Name] = newFragment;
                }

                _fragmentTypeDictionary[newFragment.GetType()].Add(newFragment);
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

        public List<T> GetFragmentsOfType<T>() where T : WldFragment
        {
            if (!_fragmentTypeDictionary.ContainsKey(typeof(T)))
            {
                return new List<T>();
            }

            return _fragmentTypeDictionary[typeof(T)].Cast<T>().ToList();
        }

        public T GetFragmentByName<T>(string fragmentName) where T : WldFragment
        {
            if (!_fragmentNameDictionary.ContainsKey(fragmentName))
            {
                return default(T);
            }

            return _fragmentNameDictionary[fragmentName] as T;
        }

        protected virtual void ProcessData()
        {
            BuildSkeletonData();
            MaterialFixer.Fix(this);
        }


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
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        public virtual void ExportData()
        {
            ExportMeshes();

            if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
            {
                ExportActors();
                ExportSkeletonAndAnimations();
            }
        }
        /// <summary>
        /// Returns a mapping of the material name to the shader type
        /// Used in exporting the bitmaps from the PFS archive
        /// </summary>
        /// <returns>Dictionary with material to shader mapping</returns>
        public List<string> GetMaskedBitmaps()
        {
            var materialLists = GetFragmentsOfType<MaterialList>();

            if (materialLists.Count == 0)
            {
                _logger.LogWarning("Cannot get material types. No texture list found.");
                return null;
            }

            List<string> maskedTextures = new List<string>();

            foreach (var list in materialLists)
            {
                foreach (var material in list.Materials)
                {
                    if (material.ShaderType != ShaderType.TransparentMasked)
                    {
                        continue;
                    }

                    maskedTextures.AddRange(material.GetAllBitmapNames(true));
                }

                if (list.AdditionalMaterials != null)
                {
                    foreach (var material in list.AdditionalMaterials)
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

        private void ExportMeshes()
        {
            if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
            {
                MeshExporter.ExportMeshes(this, _settings, _logger);
            }
            else if (_settings.ModelExportFormat == ModelExportFormat.Obj)
            {
                ActorObjExporter.ExportActors(this, _settings, _logger);
            }
            else
            {
                ActorGltfExporter.ExportActors(this, _settings, _logger);
            }
        }

        public string GetExportFolderForWldType()
        {
            switch (_wldType)
            {
                case WldType.Zone:
                case WldType.Lights:
                case WldType.ZoneObjects:
                    return GetRootExportFolder() + "/Zone/";
                case WldType.Equipment:
                    return GetRootExportFolder();
                case WldType.Objects:
                    return GetRootExportFolder() + "Objects/";
                case WldType.Sky:
                    return GetRootExportFolder();
                case WldType.Characters:
                    if (_settings.ExportCharactersToSingleFolder)
                    {
                        return GetRootExportFolder();
                    }
                    else
                    {
                        return GetRootExportFolder() + "Characters/";
                    }
                default:
                    return string.Empty;
            }
        }

        protected string GetRootExportFolder()
        {
            switch (_wldType)
            {
                case WldType.Equipment when _settings.ExportEquipmentToSingleFolder &&
                                            _settings.ModelExportFormat == ModelExportFormat.Intermediate:
                    return RootExportFolder + "equipment/";
                case WldType.Characters when (_settings.ExportCharactersToSingleFolder &&
                        _settings.ModelExportFormat == ModelExportFormat.Intermediate):
                    return RootExportFolder + "characters/";
                default:
                    return RootExportFolder + ShortnameHelper.GetCorrectZoneShortname(_zoneName) + "/";
            }
        }

        private void ExportActors()
        {
            if (GetFragmentsOfType<Actor>().Count == 0)
            {
                return;
            }

            TextAssetWriter actorWriterStatic, actorWriterSkeletal, actorWriterParticle, actorWriterSprite2d;

            if (_wldType == WldType.Equipment && _settings.ExportEquipmentToSingleFolder || _wldType == WldType.Characters)
            {
                bool isCharacters = _wldType == WldType.Characters;
                actorWriterStatic = new ActorWriterNewGlobal(ActorType.Static, GetExportFolderForWldType());
                actorWriterSkeletal = new ActorWriterNewGlobal(ActorType.Skeletal, GetExportFolderForWldType());
                actorWriterParticle = new ActorWriterNewGlobal(ActorType.Particle, GetExportFolderForWldType());
                actorWriterSprite2d = new ActorWriterNewGlobal(ActorType.Sprite, GetExportFolderForWldType());
            }
            else
            {
                actorWriterStatic = new ActorWriter(ActorType.Static);
                actorWriterSkeletal = new ActorWriter(ActorType.Skeletal);
                actorWriterParticle = new ActorWriter(ActorType.Particle);
                actorWriterSprite2d = new ActorWriter(ActorType.Sprite);
            }

            foreach (var actorFragment in GetFragmentsOfType<Actor>())
            {
                actorWriterStatic.AddFragmentData(actorFragment);
                actorWriterSkeletal.AddFragmentData(actorFragment);
                actorWriterParticle.AddFragmentData(actorFragment);
                actorWriterSprite2d.AddFragmentData(actorFragment);
            }

            string exportPath = GetExportFolderForWldType() + "actors";
            actorWriterStatic.WriteAssetToFile(exportPath + "_static.txt");
            actorWriterSkeletal.WriteAssetToFile(exportPath + "_skeletal.txt");
            actorWriterParticle.WriteAssetToFile(exportPath + "_particle.txt");
            actorWriterSprite2d.WriteAssetToFile(exportPath + "_sprite.txt");
        }

        protected void ExportSkeletonAndAnimations()
        {
            string skeletonsFolder = GetExportFolderForWldType() + "Skeletons/";
            string animationsFolder = GetExportFolderForWldType() + "Animations/";

            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();

            if (skeletons.Count == 0)
            {
                if (_wldToInject == null)
                {
                    _logger.LogWarning("Cannot export animations. No model references.");
                    return;
                }

                skeletons = _wldToInject.GetFragmentsOfType<SkeletonHierarchy>();

                if (skeletons == null)
                {
                    _logger.LogWarning("Cannot export animations. No model references.");
                    return;
                }
            }

            SkeletonHierarchyWriter skeletonWriter = new SkeletonHierarchyWriter(_wldType == WldType.Characters);
            AnimationWriter animationWriter = new AnimationWriter(_wldType == WldType.Characters);

            foreach (var skeleton in skeletons)
            {
                string filePath = skeletonsFolder + skeleton.ModelBase + ".txt";

                skeletonWriter.AddFragmentData(skeleton);

                // TODO: Put this elsewhere - what does this even do?
                if (_wldType == WldType.Characters && _settings.ExportCharactersToSingleFolder)
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

        public List<string> GetAllBitmapNames()
        {
            List<string> bitmaps = new List<string>();
            var bitmapFragments = GetFragmentsOfType<BitmapName>();
            foreach (var fragment in bitmapFragments)
            {
                bitmaps.Add(fragment.Filename);
            }

            return bitmaps;
        }

        private void BuildSkeletonData()
        {
            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();

            foreach (var skeleton in skeletons)
            {
                skeleton.BuildSkeletonData(_wldType == WldType.Characters);
            }

            (_wldToInject as WldFileCharacters)?.BuildSkeletonData();
        }
    }
}