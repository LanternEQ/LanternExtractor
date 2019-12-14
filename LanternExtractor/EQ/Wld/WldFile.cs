using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GlmSharp;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
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

        private List<BspRegion> _bspRegions;

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

        /// <summary>
        /// Is this the new WLD format? Some data types are different
        /// </summary>
        private bool _isNewWldFormat;

        private Dictionary<string, Material> GlobalCharacterMaterials;

        private Dictionary<string, CharacterModel> Models = new Dictionary<string, CharacterModel>();

        private List<BoneTransform> Frames = new List<BoneTransform>();

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

            // Character archives require a bit of "post processing" after they are instantiated
            if (_wldType == WldType.Characters)
            {
                ProcessCharacterData();
                //ProcessCharacterSkins();
            }

            ExportWldData();

            return true;
        }

        private void ProcessCharacterData()
        {
            ImportCharacters();
            ImportCharacterPalettes();
            ImportSkeletons();
            ResolveAnimations();

            foreach (var model in Models)
            {     
                FindGorillaAnimation(model.Value.AnimationBase);
                CreateSkeletonPieceHierarchy(model.Value.AnimationBase);
                ExportCharacterModel(model.Value.AnimationBase);
                ExportCharacterSkeleton(model.Value.AnimationBase);
                ExportCharacterAnimations(model.Value.AnimationBase);
            }
        }

        private void CreateSkeletonPieceHierarchy(string modelName)
        {
            CharacterModel model = Models[modelName];

            if (model == null)
            {
                return;
            }

            Dictionary<int, string> boneNames = new Dictionary<int, string>();
            RecurseBone(0, model.skeleton._pose._skeleton._tree, "", boneNames);
        }

        private void RecurseBone(int index, List<SkeletonNode> treeNodes, string runningName,
            Dictionary<int, string> paths)
        {
            SkeletonNode node = treeNodes[index];

            if (node.Name != string.Empty)
            {
                runningName += node.Name + "/";
            }

            node.FullPath = runningName.Substring(0, runningName.Length - 1);
            ;

            if (node.Children.Count == 0)
            {
                return;
            }

            foreach (var childNode in node.Children)
            {
                RecurseBone(childNode, treeNodes, runningName, paths);
            }
        }

        private void FindGorillaAnimation(string animBase)
        {
            CharacterModel model = Models[animBase];

            if (model == null)
            {
                return;
            }

            //List<BoneTrack> currentTracks = new List<BoneTrack>();
            Dictionary<string, BoneTrack> currentTracks = new Dictionary<string, BoneTrack>();

            string animationName = string.Empty;
            string modelName = string.Empty;

            foreach (var fragment in _fragmentTypeDictionary[0x13])
            {
                TrackFragment track = fragment as TrackFragment;

                if (track == null)
                {
                    continue;
                }

                if (track.TrackDefFragment.IsAssigned)
                {
                    //_logger.LogError(track.TrackDefFragment.Name);
                    continue;
                }

                string partName = track.TrackDefFragment.Name;

                string animNameCur = partName.Substring(0, 3);
                string modelNameCur = partName.Substring(3, 3);

                if (modelNameCur != animBase)
                {
                    continue;
                }

                if (animationName == string.Empty)
                {
                    animationName = animNameCur;
                    modelName = modelNameCur;
                }

                if (animationName != animNameCur || modelName != modelNameCur)
                {
                    List<BoneTrack> tracks = new List<BoneTrack>();

                    for (int i = 0; i < model.skeleton._tree.Count; ++i)
                    {
                        string partNameFromIndex = model.skeleton._treePartMap[i + 1];

                        if (currentTracks.ContainsKey(partNameFromIndex))
                        {
                            tracks.Add(currentTracks[partNameFromIndex]);
                        }
                        else
                        {
                            // One transform
                            tracks.Add(new BoneTrack {_frames = new List<BoneTransform>() {new BoneTransform()}});
                        }
                    }

                    Animation newAnimation = new Animation(animationName, tracks, FindSkeleton(modelName), null);
                    model.AddAnimation(animationName, newAnimation);
                    currentTracks.Clear();

                    animationName = animNameCur;
                    modelName = modelNameCur;
                }

                BoneTrack newTrack = new BoneTrack();
                newTrack._name = track.TrackDefFragment.Name;
                newTrack._frameCount = track.TrackDefFragment.Frames2.Count;
                newTrack._trackDef = track.TrackDefFragment;
                newTrack._frames = track.TrackDefFragment.Frames2;

                string boneName = partName.Substring(6, partName.Length - 4 - 3 - 8);

                currentTracks[boneName] = newTrack;
            }
        }

        private bool ResolveAnimations()
        {
            int misses = 0;
            bool allLoaded = true;
            foreach (var dictionaryPair in Models)
            {
                CharacterModel model = dictionaryPair.Value;

                if (model == null)
                {
                    continue;
                }

                // All characters should have skeletons, but who knows.
                Skeleton toSkel = model.skeleton;
                if (toSkel == null)
                {
                    continue;
                }

                // Copy animations from the base model, if any.
                string animBase = model.AnimationBase;
                if (!string.IsNullOrEmpty(animBase))
                {
                    Skeleton fromSkel = FindSkeleton(animBase);
                    if (fromSkel != null)
                        toSkel.CopyAnimationsFrom(fromSkel);
                    else
                        misses++;
                }

                // Load the animations into the model's animation array.
                /*string animationName = string.Empty;
                Dictionary<string, Animation> skelAnims = toSkel._animations;
                Animation animations[CharacterAnimation.eAnimCount];
                AnimationArray animArray = model.animations;
                for(int i = 0; i < (int)CharacterAnimation.eAnimCount; i++)
                {
                    animName = CharacterInfo::findAnimationName((CharacterAnimation)i);
                    if(animName)
                        animations[i] = skelAnims.value(animName);
                    else
                        animations[i] = NULL;
                }
                allLoaded &= animArray->load(animations, eAnimCount);*/
            }

            return (misses == 0) && allLoaded;
        }

        public Skeleton FindSkeleton(string modelName)
        {
            CharacterModel model = Models[modelName];
            if (model != null && model.skeleton != null)
                return model.skeleton;

            // TODO: Do we need multipack support?
            /*CharacterPack *pack = m_game->packs()->findCharacterPack(modelName);
            if(pack)
            {
                model = pack->models().value(modelName);
                if(model)
                    return model->skeleton();
            }*/
            return null;
        }

        private void ExportCharacterModel(string modelName)
        {
            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder;
            Directory.CreateDirectory(charactersExportFolder);
            
            if (!Models.ContainsKey(modelName))
            {
                return;
            }

            var gorillaModel = Models[modelName];

            if (gorillaModel == null)
            {
                return;
            }

            //gorillaModel.MainMesh.Def.ShiftSkeletonValues(gorillaModel.skeleton._tree,
            //  gorillaModel.skeleton._pose._boneTracks, mat4.Identity, 0, 0, _logger);


            File.WriteAllText(charactersExportFolder + modelName + "_mesh.txt", gorillaModel.MainMesh.Def.GetIntermediateMeshExport());

            //var textureList = gorillaModel.MainMesh.Def.MaterialList.GetMaterialListExport(_settings);
            //File.WriteAllText("gor.mtl", textureList);
        }

        private void ImportCharacters()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x14))
            {
                return;
            }

            // Iterate through all 0x14 fragments
            List<WldFragment> fragments = _fragmentTypeDictionary[0x14];

            foreach (WldFragment fragment in fragments)
            {
                ModelReference modelReference = fragment as ModelReference;

                if (modelReference == null)
                {
                    continue;
                }

                // Remove actordef from the name
                var actorName = fragment.Name.Replace("_ACTORDEF", "");

                var meshes = ListMeshes(modelReference);

                MeshReference mainMeshReference;
                MaterialList materialList;

                FindMainMesh(meshes, actorName, out mainMeshReference, out materialList);

                Mesh mainMeshDef = mainMeshReference.Mesh;

                // Create the amin mesh
                WldMesh mainMesh = new WldMesh(mainMeshDef, 0);
                WldMaterialPalette pal = mainMesh.ImportPalette(null);
                CharacterModel characterModel = new CharacterModel(mainMesh);
                int skinId = 0;

                foreach (MeshReference meshReference in meshes)
                {
                    Mesh mesh = meshReference.Mesh;

                    if (mesh == null)
                    {
                        continue;
                    }

                    if (mesh.MaterialList != materialList)
                    {
                        // Something bad
                        continue;
                    }

                    characterModel.AddPart(mesh, skinId);
                    pal.AddMeshMaterials(mesh, skinId);
                }

                // VERIFY ALL MODELS IN THE 0x14 fragment

                FindModelRaceGender(characterModel, actorName);
                Models[actorName] = characterModel;
            }

            // look for alternate meshes (e.g. heads)
            foreach (WldFragment meshFragment in _fragmentTypeDictionary[0x36])
            {
                Mesh mesh = meshFragment as Mesh;

                if (mesh == null)
                {
                    continue;
                }

                if (mesh.Handled)
                {
                    continue;
                }

                string actorName;
                string meshName;
                int skinId;

                ExplodeMeshName(mesh.Name, out actorName, out meshName, out skinId);

                CharacterModel model = Models[actorName];

                if (model == null)
                {
                    continue;
                }

                WldMaterialPalette pal = model.MainMesh.Palette;

                int defaultSkinMask = model.GetSkinMask(0);

                List<WldMesh> parts = model.Meshes;

                for (int i = 0; i < parts.Count; ++i)
                {
                    if (!model.IsPartUsed(defaultSkinMask, i))
                    {
                        continue;
                    }

                    WldMesh part = parts[i];

                    string actorName2;
                    string meshName2;
                    int skinId2;

                    ExplodeMeshName(part.Def.Name, out actorName2, out meshName2, out skinId2);

                    if ((meshName2 == meshName) && (skinId2 != skinId))
                    {
                        model.ReplacePart(mesh, skinId, i);
                        pal.AddMeshMaterials(mesh, skinId);
                    }
                }
            }
        }

        List<MeshReference> ListMeshes(ModelReference def)
        {
            List<MeshReference> meshes = new List<MeshReference>();

            if (def == null)
                return meshes;

            // TODO: Set this up to work with all types of fragments
            foreach (HierSpriteFragment skeletonReference in def.SkeletonReferences)
            {
                // Should be meshes in here
                foreach (MeshReference meshReference in skeletonReference.HierSpriteDefFragment.Meshes)
                {
                    meshes.Add(meshReference);
                }

                var tree = skeletonReference.HierSpriteDefFragment.Tree;
                foreach (var node in tree)
                {
                    if (node.Mesh != null)
                    {
                        meshes.Add(node.Mesh);
                    }
                }
            }

            return meshes;
        }

        private void ExportCharacterSkeleton(string modelName)
        {
            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder;
            Directory.CreateDirectory(charactersExportFolder);
            
            CharacterModel characterModel = Models[modelName];

            StringBuilder skeletonExport = new StringBuilder();

            skeletonExport.AppendLine("# Lantern Skeleton Test Export");
            skeletonExport.AppendLine("# Total verts: " + characterModel.MainMesh.Def.Vertices.Count);

            for (var i = 0; i < characterModel.skeleton._tree.Count; i++)
            {
                var node = characterModel.skeleton._tree[i];
                skeletonExport.Append(node.Name);
                skeletonExport.Append(",");
                skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[0].Translation.x);
                skeletonExport.Append(",");
                skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[0].Translation.z);
                skeletonExport.Append(",");
                skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[0].Translation.y);
                skeletonExport.Append(",");
                skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[0].Rotation.EulerAngles.x);
                skeletonExport.Append(",");
                skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[0].Rotation.EulerAngles.z);
                skeletonExport.Append(",");
                skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[0].Rotation.EulerAngles.y);

                if (!characterModel.MainMesh.Def.MobPieces.ContainsKey(i))
                {
                    skeletonExport.Append(",-1");
                    skeletonExport.Append(",-1");
                }
                else
                {
                    skeletonExport.Append(",");
                    skeletonExport.Append(characterModel.MainMesh.Def.MobPieces[i].Start);
                    skeletonExport.Append(",");
                    skeletonExport.Append(characterModel.MainMesh.Def.MobPieces[i].Count);
                }

                if (node.Children.Count == 0)
                {
                    skeletonExport.AppendLine();
                    continue;
                }

                skeletonExport.Append(",");

                for (var j = 0; j < node.Children.Count; j++)
                {
                    if (j != 0)
                    {
                        skeletonExport.Append(";");
                    }

                    var boneId = node.Children[j];
                    skeletonExport.Append(boneId);
                }

                skeletonExport.AppendLine();
            }

            File.WriteAllText(charactersExportFolder + modelName + "_skeleton.txt", skeletonExport.ToString());
        }

        private void ExportCharacterAnimations(string modelName)
        {
            if (!Models.ContainsKey(modelName))
            {
                return;
            }

            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder;
            Directory.CreateDirectory(charactersExportFolder);
            
            CharacterModel characterModel = Models[modelName];
            
            foreach (Animation animation in characterModel.animations.Values)
            {
                StringBuilder skeletonExport = new StringBuilder();

                skeletonExport.AppendLine("# Lantern Animation Test Export: " + animation.Name);
                skeletonExport.AppendLine("# Total frames: " + animation._frameCount);

                for (var i = 0; i < characterModel.skeleton._tree.Count; ++i)
                {
                    for (var j = 0; j < animation._frameCount; j++)
                    {
                        int frameIndex = j;

                        if (animation._boneTracks[i]._frames.Count == 1)
                        {
                            frameIndex = 0;
                        }

                        var node = characterModel.skeleton._tree[i];

                        skeletonExport.Append(node.FullPath);
                        skeletonExport.Append(",");
                        skeletonExport.Append(j);
                        skeletonExport.Append(",");
                        skeletonExport.Append(animation._boneTracks[i]._frames[frameIndex]
                            .Translation.x);
                        skeletonExport.Append(",");
                        skeletonExport.Append(animation._boneTracks[i]._frames[frameIndex]
                            .Translation.z);
                        skeletonExport.Append(",");
                        skeletonExport.Append(animation._boneTracks[i]._frames[frameIndex]
                            .Translation.y);
                        skeletonExport.Append(",");
                        skeletonExport.Append(animation._boneTracks[i]._frames[frameIndex]
                            .Rotation.x);
                        skeletonExport.Append(",");
                        skeletonExport.Append(animation._boneTracks[i]._frames[frameIndex]
                            .Rotation.z);
                        skeletonExport.Append(",");
                        skeletonExport.Append(animation._boneTracks[i]._frames[frameIndex]
                            .Rotation.y);
                        skeletonExport.Append(",");
                        skeletonExport.Append(animation._boneTracks[i]._frames[frameIndex]
                            .Rotation.w);
                        skeletonExport.AppendLine();
                    }
                }

                File.WriteAllText(charactersExportFolder + characterModel.AnimationBase + "_" + animation.Name +".txt", skeletonExport.ToString());
            }
        }

        private void ExportPoseAnimation(string modelName)
        {
            CharacterModel characterModel = Models[modelName];

            StringBuilder skeletonExport = new StringBuilder();

            skeletonExport.AppendLine("# Lantern Animation Test Export");
            skeletonExport.AppendLine("# Total frames: 1");

            for (var i = 0; i < characterModel.skeleton._tree.Count; ++i)
            {
                for (var j = 0; j < 1; j++)
                {
                    int frameIndex = 0;

                    var node = characterModel.skeleton._tree[i];

                    skeletonExport.Append(node.FullPath);
                    skeletonExport.Append(",");
                    skeletonExport.Append(j);
                    skeletonExport.Append(",");
                    skeletonExport.Append(
                        characterModel.skeleton._pose._boneTracks[i]._frames[frameIndex].Translation.x);
                    skeletonExport.Append(",");
                    skeletonExport.Append(
                        characterModel.skeleton._pose._boneTracks[i]._frames[frameIndex].Translation.z);
                    skeletonExport.Append(",");
                    skeletonExport.Append(
                        characterModel.skeleton._pose._boneTracks[i]._frames[frameIndex].Translation.y);
                    skeletonExport.Append(",");
                    skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[frameIndex].Rotation
                        .EulerAngles.x);
                    skeletonExport.Append(",");
                    skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[frameIndex].Rotation
                        .EulerAngles.z);
                    skeletonExport.Append(",");
                    skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[frameIndex].Rotation
                        .EulerAngles.y);
                    skeletonExport.AppendLine();
                }
            }


            File.WriteAllText(modelName + "_pose.txt", skeletonExport.ToString());
        }

        static bool FindMainMesh(List<MeshReference> meshes, string actorName,
            out MeshReference mainFragment, out MaterialList mainMaterialList)
        {
            string mainMeshName = actorName + "_DMSPRITEDEF";
            foreach (MeshReference meshRef in meshes)
            {
                Mesh mesh = meshRef.Mesh;
                string meshName = mesh != null ? mesh.Name : string.Empty;
                if (meshName == mainMeshName || meshes.Count == 1)
                {
                    mainFragment = meshRef;
                    mainMaterialList = mesh != null ? mesh.MaterialList : null;
                    return true;
                }
            }

            mainFragment = null;
            mainMaterialList = null;

            return false;

            // Special case for the 'Invisible Man' model which has no mesh definition.
            /*if((actorName == "IVM") && (meshes.size() > 0))
            {
                meshOut = meshes[0];
                palDefOut = NULL;
                return true;
            }
            return false;*/
        }

        bool FindModelRaceGender(CharacterModel model, string modelName)
        {
            // This deals with parsing the CSV file and figuring out data about the race
            /*RaceInfo info;
             for(int r = 0; r <= RACE_MAX; r++)
             {
                 RaceID race = (RaceID)r;
                 if(!info.findByID(race))
                     continue;
                 if(modelName == info.modelMale)
                 {
                     model->setRace(race);
                     model->setGender(eGenderMale);
                     model->setAnimBase(info.animMale);
                     return true;
                 }
                 else if(modelName == info.modelFemale)
                 {
                     model->setRace(race);
                     model->setGender(eGenderFemale);
                     model->setAnimBase(info.animFemale);
                     return true;
                 }
             }*/

            model.SetRace(RaceId.Human);
            model.SetGender(GenderId.eGenderMale);
            model.SetAnimBase(model.MainMesh.Def.Name.Substring(0, 3));

            return false;
        }

        static bool ExplodeMeshName(string defName, out string actorName,
            out string meshName, out int skinId)
        {
            // e.g. defName == 'ELEHE00_DMSPRITEDEF'
            // 'ELE' : character
            // 'HE' : mesh
            // '00' : skin ID
            Regex expression = new Regex("^(\\w{3})(.*)(\\d{2})_DMSPRITEDEF$");
            if (expression.IsMatch(defName))
            {
                var match = expression.Match(defName);
                actorName = match.Groups[1].ToString();

                // TODO: verify this
                meshName = match.Groups[2].ToString();
                skinId = Convert.ToInt32(match.Groups[3].ToString());
                return true;
            }

            actorName = string.Empty;
            meshName = string.Empty;
            skinId = 0;
            return false;
        }

        private void ImportCharacterPalettes()
        {
            foreach (WldFragment materialFragments in _fragmentTypeDictionary[0x30])
            {
                Material material = materialFragments as Material;

                if (material == null)
                {
                    continue;
                }

                if (material.IsHandled)
                {
                    continue;
                }

                string charName;
                string partName;
                int skinId;

                if (WldMaterialPalette.ExplodeName(material.Name, out charName, out skinId, out partName))
                {
                    CharacterModel model = Models[charName];

                    if (model == null)
                    {
                        continue;
                    }

                    WldMaterialPalette palette = model.MainMesh.Palette;

                    model.GetSkinMask(skinId, true);
                    WldMaterialSlot matSlot = palette.SlotByName(partName);
                    if (matSlot != null)
                    {
                        matSlot.AddSkinMaterial(skinId, material);
                    }
                }
            }
        }

        private void ImportSkeletons()
        {
            // count skeleton track frames
            List<BoneTrack> boneTracks = new List<BoneTrack>();
            Dictionary<int, int> trackMap = new Dictionary<int, int>();
            int frameCount = 0;

            for (int i = 0; i < _fragmentTypeDictionary[0x12].Count; i++)
            {
                TrackDefFragment trackDef = _fragmentTypeDictionary[0x12][i] as TrackDefFragment;

                if (trackDef == null)
                {
                    continue;
                }

                BoneTrack boneTrack = new BoneTrack();
                boneTrack._name = trackDef.Name;
                boneTrack._frameCount = trackDef.Frames2.Count;
                boneTrack._trackDef = trackDef;
                frameCount += boneTrack._frameCount;
                boneTracks.Add(boneTrack);
                trackMap[trackDef.Index] = i;

                if (trackDef.Name == "C05RHIWA_TRACKDEF")
                {
                }
            }

            // import skeleton tracks
            int current = 0;

            // Create bone transform space
            for (int i = 0; i < frameCount; ++i)
            {
                Frames.Add(new BoneTransform());
            }

            for (int i = 0; i < _fragmentTypeDictionary[0x12].Count; i++)
            {
                TrackDefFragment trackDef = _fragmentTypeDictionary[0x12][i] as TrackDefFragment;

                if (trackDef == null)
                {
                    continue;
                }

                BoneTrack boneTrack = boneTracks[i];

                List<BoneTransform> frames = new List<BoneTransform>();

                for (int j = 0; j < boneTrack._frameCount; j++)
                {
                    Frames[current] = trackDef.Frames2[j];
                    frames.Add(trackDef.Frames2[j]);
                }

                boneTrack._frames = frames;
                current += boneTrack._frameCount;
            }

            // import skeletons which contain the pose animation
            foreach (WldFragment fragment in _fragmentTypeDictionary[0x10])
            {
                HierSpriteDefFragment skelDef = fragment as HierSpriteDefFragment;

                if (skelDef == null)
                {
                    continue;
                }

                string actorName = skelDef.Name.Replace("_HS_DEF", "");

                CharacterModel model = Models[actorName];
                if (model == null)
                    continue;

                List<BoneTrack> tracks = new List<BoneTrack>();
                Dictionary<int, string> names = new Dictionary<int, string>();

                foreach (SkeletonNode node in skelDef.Tree)
                {
                    int trackId = trackMap[node.Track.TrackDefFragment.Index];
                    tracks.Add(boneTracks[trackId]);

                    string partName = node.Name;
                    partName = partName.Substring(3, partName.Length - 4 - 3);

                    names[tracks.Count] = partName;
                    boneTracks[trackId]._trackDef.IsAssigned = true;
                }

                model.SetSkeleton(new Skeleton(skelDef.Tree, names, tracks, skelDef.BoundingRadius));
            }
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
        /// Processes each material list to create the texture slots
        /// Adds the alternate skin materials to their own skin dictionary
        /// </summary>
        private void ProcessCharacterSkins()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x31))
            {
                _logger.LogWarning("Cannot process character skins. No material list found.");
                return;
            }

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
                    Console.WriteLine("Can't find parent material list: " + material.Name);
                    continue;
                }

                // _logger.LogError("Match: " + material.Name + " to: " + parentList.Name);

                parentList.AddMaterialToSkins(material, _logger);
            }
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        public void ExportWldData()
        {
            if (_wldType == WldType.Zone)
            {
                ExportZoneMeshes();
                ExportMaterialList();
                ExportBspTree();
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
            {
                _logger.LogWarning("Cannot export zone meshes. No meshes found.");
                return;
            }

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
            Material lastUsedMaterial = null;

            for (int i = 0; i < zoneMeshes.Count; ++i)
            {
                Mesh zoneMesh = zoneMeshes[i];

                if (useMeshGroups)
                {
                    zoneExport.AppendLine("g " + i);
                }

                List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedMaterial,
                    ObjExportType.Textured, out addedVertices, out lastUsedMaterial, _settings, _logger);

                if (outputStrings == null || outputStrings.Count == 0)
                {
                    _logger.LogError("Mesh has no valid output: " + zoneMesh);
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
                lastUsedMaterial = null;

                for (int i = 0; i < zoneMeshes.Count; ++i)
                {
                    Mesh zoneMesh = zoneMeshes[i];

                    if (useMeshGroups)
                    {
                        collisionExport.AppendLine("g " + i);
                    }

                    List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedMaterial,
                        ObjExportType.Collision, out addedVertices, out lastUsedMaterial, _settings, _logger);

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

            // Theoretically, there should only be one texture list here
            // Exceptions include sky.s3d
            for (int i = 0; i < _fragmentTypeDictionary[0x31].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x31][i] is MaterialList materialList))
                {
                    continue;
                }

                materialsExport.Append(materialList.GetMaterialListExport(_settings));
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
            {
                _logger.LogWarning("Cannot export zone object meshes. No meshes found.");
                return;
            }

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
                Material lastUsedMaterial = null;

                List<string> meshStrings = objectMesh.GetMeshExport(0, lastUsedMaterial,
                    ObjExportType.Textured, out addedVertices, out lastUsedMaterial, _settings, _logger);

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

                    File.WriteAllText(_zoneName + "/" + _zoneName + "_animated_meshes.txt",
                        animatedMeshesHeader + animatedMeshInfo.ToString());
                }

                // Collision mesh
                if (objectMesh.ExportSeparateCollision)
                {
                    var collisionExport = new StringBuilder();
                    collisionExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Collision Mesh - " +
                                               fixedObjectName);

                    addedVertices = 0;
                    lastUsedMaterial = null;
                    meshStrings = objectMesh.GetMeshExport(0, lastUsedMaterial, ObjExportType.Collision,
                        out addedVertices, out lastUsedMaterial, _settings, _logger);

                    if (meshStrings == null || meshStrings.Count == 0)
                    {
                        continue;
                    }

                    File.WriteAllText(
                        objectsExportFolder + fixedObjectName + "_collision" + LanternStrings.ObjFormatExtension,
                        meshStrings[0]);
                }

                // Materials
                var materialsExport = new StringBuilder();
                materialsExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material Definitions");
                materialsExport.Append(objectMesh.MaterialList.GetMaterialListExport(_settings));

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
            if (!_fragmentTypeDictionary.ContainsKey(0x15))
            {
                _logger.LogWarning("Cannot export object instance list. No object instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/";

            Directory.CreateDirectory(zoneExportFolder);

            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};

            var objectListExport = new StringBuilder();

            objectListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Instances");
            objectListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                        "ModelName, PosX, PosY, PosZ, RotX, RotY, RotZ, ScaleX, ScaleY, ScaleZ");

            for (int i = 0; i < _fragmentTypeDictionary[0x15].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x15][i] is ObjectInstance objectLocation))
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
                objectListExport.Append(objectLocation.Rotation.z.ToString(format));
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
            if (!_fragmentTypeDictionary.ContainsKey(0x28))
            {
                _logger.LogWarning("Unable to export light instance list. No instances found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/";

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

        private void ExportBspTree()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x21))
            {
                _logger.LogWarning("Cannot export BSP tree. No tree found.");
                return;
            }

            if (_fragmentTypeDictionary[0x21].Count != 1)
            {
                _logger.LogWarning("Cannot export BSP tree. Incorrect number of trees found.");
                return;
            }

            if (_bspRegions.Count == 0)
            {
                return;
            }

            if (_fragmentTypeDictionary.ContainsKey(0x29))
            {
                foreach (WldFragment fragment in _fragmentTypeDictionary[0x29])
                {
                    RegionFlag region = fragment as RegionFlag;

                    if (region == null)
                    {
                        continue;
                    }

                    region.LinkRegionType(_bspRegions);
                }
            }

            string zoneExportFolder = _zoneName + "/";

            Directory.CreateDirectory(zoneExportFolder);

            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};

            BspTree bspTree = _fragmentTypeDictionary[0x21][0] as BspTree;

            if (bspTree == null)
            {
                return;
            }

            StringBuilder bspTreeExport = new StringBuilder();
            bspTreeExport.AppendLine(LanternStrings.ExportHeaderTitle + "BSP Tree");
            bspTreeExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                     "Normal nodes: NormalX, NormalY, NormalZ, SplitDistance, LeftNodeId, RightNodeId");
            bspTreeExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                     "Leaf nodes: BSPRegionId, RegionType");
            bspTreeExport.AppendLine(bspTree.GetBspTreeExport(_fragmentTypeDictionary[0x22], _logger));

            File.WriteAllText(zoneExportFolder + _zoneName + "_bsp_tree.txt", bspTreeExport.ToString());
        }

        /// <summary>
        /// Exports all meshes in the archive. If the mesh belongs to a model, it exports additional information
        /// </summary>
        private void ExportCharacterMeshes()
        {
            return;
            if (!_fragmentTypeDictionary.ContainsKey(0x36))
            {
                _logger.LogWarning("Cannot export character meshes. No meshes found.");
                return;
            }

            foreach (WldFragment meshFragment in _fragmentTypeDictionary[0x36])
            {
                if (!(meshFragment is Mesh mesh))
                {
                    continue;
                }

                // Find the model reference
                ModelReference actorReference;

                bool isMainModel = FindModelReference(mesh.Name.Split('_')[0] + "_ACTORDEF", out actorReference);

                if (!isMainModel && !FindModelReference(mesh.Name.Substring(0, 3) + "_ACTORDEF", out actorReference))
                {
                    // _logger.LogError("Cannot export character model: " + mesh.Name);
                    continue;
                }

                // If this is a skeletal model, shift the values to get the default pose (things like boats have a skeleton but no references)
                if (actorReference.SkeletonReferences.Count != 0)
                {
                    HierSpriteDefFragment skeleton = actorReference.SkeletonReferences[0].HierSpriteDefFragment;

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
                _logger.LogWarning("Cannot find model reference for " + modelName);
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
            return;
            string exportDirectory = _zoneName + "/" + LanternStrings.ExportCharactersFolder;
            Directory.CreateDirectory(exportDirectory);

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

            File.WriteAllText(
                exportDirectory + Mesh.FixCharacterMeshName(mesh.Name, isMainModel) + LanternStrings.ObjFormatExtension,
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
            }
        }
    }
}