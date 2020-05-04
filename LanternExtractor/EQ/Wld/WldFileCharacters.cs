using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileCharacters : WldFile
    {
        public Dictionary<string, string> AnimationModelLink;

        private Dictionary<string, Material> GlobalCharacterMaterials;

        protected Dictionary<string, CharacterModel> Models = new Dictionary<string, CharacterModel>();

        protected List<BoneTransform> Frames = new List<BoneTransform>();

        private Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();

        public WldFileCharacters(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
            ParseModelAnimationLink();
        }

        private void ParseModelAnimationLink()
        {
            string filename = "models.txt";
            if (!File.Exists(filename))
            {
                _logger.LogError("WldFileCharacters: No models.txt file found.");
                return;
            }

            AnimationModelLink = new Dictionary<string, string>();

            string fileText = File.ReadAllText(filename);
            List<List<string>> parsedText = TextParser.ParseTextByDelimitedLines(fileText, ',', '#');

            foreach (var line in parsedText)
            {
                if (line.Count < 5)
                {
                    continue;
                }
                
                AnimationModelLink[line[2]] = line[4];
            }        
        }
        
        private void GetAnimationModelLink(CharacterModel model, string modelName)
        {
            if (AnimationModelLink == null || !AnimationModelLink.ContainsKey(modelName))
            {
                return;
            }

            var animationBase = AnimationModelLink[modelName];
            model.SetAnimationBase(animationBase == string.Empty ? modelName : animationBase);
        }

        protected override void ProcessData()
        {
            ImportCharacters();
            ImportCharacterPalettes();
            ImportSkeletons();
            ResolveAnimations();
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportData()
        {
            base.ExportData();

            if (_wldToInject != null)
            {
                Models = (_wldToInject as WldFileCharacters)?.Models;
            }
            
            foreach (var model in Models)
            {
                var animationBase = model.Key;

                if (model.Key == "IVM")
                {
                    continue;
                }
                
                CreateSkeletonPieceHierarchy(animationBase);
                FindAnimations(animationBase);
                ExportCharacterMesh(animationBase);
                ExportCharacterSkeleton(animationBase);
            }

            FindAllAnimations();
            ExportAllAnimations();
            ExportCharacterList();
            //ExportSkeletons();
        }

        private void ExportAllAnimations()
        {
            if (_animations == null || _animations.Count == 0)
            {
                return;
            }
            
            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder + "Animations/";
            Directory.CreateDirectory(charactersExportFolder);

            string lastAnimationModel = string.Empty;
            Skeleton skeleton = null;

            foreach (var animation in _animations)
            {
                string modelName = animation.Key.Substring(0, 3);
                
                if (modelName != lastAnimationModel)
                {
                    lastAnimationModel = modelName;
                        
                    // Find the associated character model
                    if (Models.ContainsKey(modelName))
                    {
                        CharacterModel characterModel = Models[modelName];
                        skeleton = characterModel.Skeleton;
                    }
                    else
                    {
                        // Find that mother
                        foreach (var characterModel in Models)
                        {
                            if (characterModel.Value.AnimationBase == modelName)
                            {
                                skeleton = characterModel.Value.Skeleton;
                                break;
                            }
                        }
                    }
                }

                if (skeleton == null)
                {
                    _logger.LogError("Unable to export animation because there was no associated skeleton: " +
                                     animation.Key);
                    continue;
                }

                string exportString = GetAnimationString(animation.Value, skeleton);
                File.WriteAllText(charactersExportFolder + animation.Key + ".txt", exportString);
            }
        }

        private void ExportCharacterList()
        {
            if (_wldToInject != null)
            {
                return;
            }

            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.ModelReference))
            {
                return;
            }
            
            string zoneExportFolder = _zoneName + "/";

            CharacterListExporter exporter = new CharacterListExporter(_fragmentTypeDictionary[FragmentType.ModelReference].Count);
            
            foreach(WldFragment fragment in _fragmentTypeDictionary[FragmentType.ModelReference])
            {
                exporter.AddFragmentData(fragment);
            }
            
            exporter.WriteAssetToFile(zoneExportFolder + _zoneName + "_characters.txt");
        }

        private void ImportCharacters()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.ModelReference))
            {
                return;
            }

            List<WldFragment> fragments = _fragmentTypeDictionary[FragmentType.ModelReference];

            foreach (WldFragment fragment in fragments)
            {
                ModelReference modelReference = fragment as ModelReference;

                if (modelReference == null)
                {
                    continue;
                }

                string actorName = fragment.Name.Replace("_ACTORDEF", "");

                List<MeshReference> meshes = GetAllMeshesForModel(modelReference);

                MeshReference mainMeshReference;
                MaterialList materialList;

                if (!FindMainMesh(meshes, actorName, out mainMeshReference, out materialList))
                {
                    _logger.LogError("Unable to find main mesh for model: " + actorName);
                    continue;
                }

                Mesh mainMeshDef = mainMeshReference.Mesh;

                // Create the main mesh
                WldMesh mainMesh = new WldMesh(mainMeshDef, 0);
                WldMaterialPalette pal = mainMesh.ImportPalette(null);
                CharacterModel characterModel = new CharacterModel(actorName, mainMesh);
                int skinId = 0;

                foreach (MeshReference meshReference in meshes)
                {
                    Mesh mesh = meshReference.Mesh;

                    if (mesh == null)
                    {
                        continue;
                    }

                    if (meshReference.Mesh == mainMesh.Def)
                    {
                        continue;
                    }

                    if (mesh.MaterialList != materialList)
                    {
                        continue;
                    }

                    characterModel.AddPart(mesh, skinId);
                    pal.AddMeshMaterials(mesh, skinId);
                    mesh.Handled = true;
                }

                // VERIFY ALL MODELS IN THE 0x14 fragment

                GetAnimationModelLink(characterModel, actorName);
                Models[actorName] = characterModel;
            }

            // Look for alternate meshes (e.g. heads)
            foreach (WldFragment meshFragment in _fragmentTypeDictionary[FragmentType.Mesh])
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

                if (!Models.ContainsKey(actorName))
                {
                    continue;
                }

                CharacterModel model = Models[actorName];

                if (model == null)
                {
                    continue;
                }

                WldMaterialPalette pal = model.MainMesh.Palette;

                int defaultSkinMask = model.AddSkin(0);

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

        /// <summary>
        /// Iterates through each material and finds the corresponding model
        /// </summary>
        private void ImportCharacterPalettes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Material))
            {
                return;
            }

            foreach (WldFragment materialFragments in _fragmentTypeDictionary[FragmentType.Material])
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

                if (!WldMaterialPalette.ExplodeName2(material.Name, out charName, out skinId, out partName))
                {
                    continue;
                }

                if (!Models.ContainsKey(charName))
                {
                    _logger.LogError("WldFileCharacter: Unable to find model: " + charName);
                    continue;
                }

                Models[charName].AddNewSkin(partName, skinId, material);

                if (material.Name.StartsWith("FUNHE"))
                {
                }
                
                CharacterModel model = Models[charName];
                WldMaterialPalette palette = model.MainMesh.Palette;
                model.AddSkin(skinId, true);

                WldMaterialSlot materialSlot = palette.SlotByName(partName);

                if (materialSlot != null)
                {
                    materialSlot.AddSkinMaterial(skinId, material);
                }
            }
        }

        private void ImportSkeletons()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.TrackDefFragment))
            {
                return;
            }
            
            // count skeleton track frames
            List<BoneTrack> boneTracks = new List<BoneTrack>();
            Dictionary<int, int> trackMap = new Dictionary<int, int>();
            int frameCount = 0;

            for (int i = 0; i < _fragmentTypeDictionary[FragmentType.TrackDefFragment].Count; i++)
            {
                TrackDefFragment trackDef = _fragmentTypeDictionary[FragmentType.TrackDefFragment][i] as TrackDefFragment;

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
            }

            // import skeleton tracks
            int current = 0;

            // Create bone transform space
            for (int i = 0; i < frameCount; ++i)
            {
                Frames.Add(new BoneTransform());
            }

            for (int i = 0; i < _fragmentTypeDictionary[FragmentType.TrackDefFragment].Count; i++)
            {
                TrackDefFragment trackDef = _fragmentTypeDictionary[FragmentType.TrackDefFragment][i] as TrackDefFragment;

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

            if (_fragmentTypeDictionary.ContainsKey(FragmentType.SkeletonHierarchy))
            {
                // import skeletons which contain the pose animation
                foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.SkeletonHierarchy])
                {
                    SkeletonHierarchy skelDef = fragment as SkeletonHierarchy;

                    if (skelDef == null)
                    {
                        continue;
                    }

                    string actorName = skelDef.Name.Replace("_HS_DEF", "");

                    if (actorName == "IVM")
                    {
                        continue;
                    }

                    if (!Models.ContainsKey(actorName))
                    {
                        continue;
                    }
                    
                    
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
                        partName = Regex.Replace(partName, @"_DAG$", String.Empty);

                        if (partName.Length == 2)
                        {
                            partName = "";
                        }
                        else
                        {
                            partName = partName.Substring(3, partName.Length - 3);
                        }


                        names[tracks.Count] = partName;
                        boneTracks[trackId]._trackDef.IsAssigned = true;
                    }

                    model.SetSkeleton(new Skeleton(skelDef.Tree, names, tracks, skelDef.BoundingRadius));
                }
            }
        }

        private void CreateSkeletonPieceHierarchy(string modelName)
        {
            CharacterModel model = Models[modelName];

            if (model == null || model.Skeleton == null)
            {
                return;
            }

            Dictionary<int, string> boneNames = new Dictionary<int, string>();
            RecurseBone(0, model.Skeleton._pose._skeleton._tree, string.Empty, string.Empty, boneNames);
        }

        private void RecurseBone(int index, List<SkeletonNode> treeNodes, string runningName, string runningIndex,
            Dictionary<int, string> paths)
        {
            SkeletonNode node = treeNodes[index];

            if (node.Name != string.Empty)
            {
                string fixedName = node.Name.Replace("_DAG", "");

                if (fixedName.Length >= 3)
                {
                    runningName += "{MOD}" + node.Name.Replace("_DAG", "").Substring(3) + "/";
                }
            }

            if (node.Name != string.Empty)
            {
                runningIndex += node.Index + "/";
            }

            if (runningName.Length >= 1)
            {
                node.FullPath = runningName.Substring(0, runningName.Length - 1);
            }

            if (runningIndex.Length >= 1)
            {
                node.FullIndexPath = runningIndex.Substring(0, runningIndex.Length - 1);
            }

            if (node.Children.Count == 0)
            {
                return;
            }

            foreach (var childNode in node.Children)
            {
                RecurseBone(childNode, treeNodes, runningName, runningIndex, paths);
            }
        }

        /// <summary>
        /// Finds and groups all character animations
        /// </summary>
        private void FindAllAnimations()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.TrackFragment))
            {
                return;
            }

            Dictionary<string, BoneTrack> currentTracks = new Dictionary<string, BoneTrack>();

            string animationName = string.Empty;
            string modelName = string.Empty;

            // Loop through each track
            foreach (var fragment in _fragmentTypeDictionary[FragmentType.TrackFragment])
            {
                TrackFragment track = fragment as TrackFragment;

                if (track == null)
                {
                    continue;
                }

                // Ignore fragment of the skeleton POS
                if (track.TrackDefFragment.IsAssigned)
                {
                    continue;
                }

                // Break apart the name
                string partName = track.TrackDefFragment.Name;
                string animNameCur = partName.Substring(0, 3);
                string modelNameCur = partName.Substring(3, 3);

                if (!Models.ContainsKey(modelNameCur))
                {
                    _logger.LogError($"Cannot find model {modelNameCur} for animation {animNameCur}.");
                    continue;
                }

                CharacterModel model = Models[modelNameCur];

                if (animationName == string.Empty)
                {
                    animationName = animNameCur;
                    modelName = modelNameCur;
                }

                if (animationName != animNameCur || modelName != modelNameCur)
                {
                    if (Models.ContainsKey(modelName))
                    {
                        var oldModel = Models[modelName];

                        List<BoneTrack> tracks = new List<BoneTrack>();

                        for (int i = 0; i < oldModel.Skeleton._tree.Count; ++i)
                        {
                            string partNameFromIndex = oldModel.Skeleton._treePartMap[i + 1];

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

                        _animations[modelName + "_" + animationName] = newAnimation;
                    }

                    currentTracks.Clear();

                    animationName = animNameCur;
                    modelName = modelNameCur;
                }

                BoneTrack newTrack = new BoneTrack
                {
                    _name = track.TrackDefFragment.Name,
                    _frameCount = track.TrackDefFragment.Frames2.Count,
                    _trackDef = track.TrackDefFragment,
                    _frames = track.TrackDefFragment.Frames2
                };

                string boneName = partName.Substring(6, partName.Length - 15);

                currentTracks[boneName] = newTrack;
            }

            // Add all pose animations
            foreach (var model in Models)
            {
                if (model.Key == "IVM")
                {
                    continue;
                }

                if (model.Value.Skeleton == null)
                {
                    continue;
                }
                
                _animations[model.Value.ModelBase + "_" + "POS"] = model.Value.Skeleton._pose;
            }
        }

        /// <summary>
        /// Finds animations for each model and adds to the model
        /// </summary>
        /// <param name="modelName1"></param>
        private void FindAnimations(string modelName1)
        {
            if (!Models.ContainsKey(modelName1))
            {
                return;
            }

            CharacterModel model = Models[modelName1];

            if (model == null)
            {
                return;
            }

            string animationbase = model.AnimationBase;

            Dictionary<string, BoneTrack> currentTracks = new Dictionary<string, BoneTrack>();

            string animationName = string.Empty;
            string modelName = string.Empty;

            foreach (var fragment in _fragmentTypeDictionary[FragmentType.TrackFragment])
            {
                TrackFragment track = fragment as TrackFragment;

                if (track == null)
                {
                    continue;
                }

                if (track.TrackDefFragment.IsAssigned)
                {
                    continue;
                }

                string partName = track.TrackDefFragment.Name;

                string animNameCur = partName.Substring(0, 3);
                string modelNameCur = partName.Substring(3, 3);

                // TODO: FIX THIS SHIT
                if (modelNameCur != animationbase)
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

                    for (int i = 0; i < model.Skeleton._tree.Count; ++i)
                    {
                        string partNameFromIndex = model.Skeleton._treePartMap[i + 1];

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
                    //_animations[modelNameCur + "_" + animationName] = newAnimation;
                    currentTracks.Clear();

                    animationName = animNameCur;
                    modelName = modelNameCur;
                }

                BoneTrack newTrack = new BoneTrack
                {
                    _name = track.TrackDefFragment.Name,
                    _frameCount = track.TrackDefFragment.Frames2.Count,
                    _trackDef = track.TrackDefFragment,
                    _frames = track.TrackDefFragment.Frames2
                };

                string boneName = partName.Substring(6, partName.Length - 15);

                currentTracks[boneName] = newTrack;
            }

            if (model.Skeleton != null)
            {
                model.AddAnimation("POS", model.Skeleton._pose);
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
                Skeleton toSkel = model.Skeleton;

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

        private Skeleton FindSkeleton(string modelName)
        {
            if (!Models.ContainsKey(modelName))
            {
                return null;
            }

            CharacterModel model = Models[modelName];

            if (model != null && model.Skeleton != null)
                return model.Skeleton;

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

        private void ExportCharacterMesh(string modelName)
        {
            if (_wldToInject != null)
            {
                return;
            }
            
            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder + "Meshes/";
            Directory.CreateDirectory(charactersExportFolder);

            if (!Models.ContainsKey(modelName))
            {
                return;
            }

            var model = Models[modelName];

            if (model == null)
            {
                return;
            }

            //model.MainMesh.Def.ShiftSkeletonValues(model.Skeleton._tree,
            //  model.Skeleton._pose._boneTracks, mat4.Identity, 0, 0, _logger);


            File.WriteAllText(charactersExportFolder + modelName + "_mesh.txt",
                model.MainMesh.Def.GetIntermediateMeshExport(-1, model.NewSkins));
            /*File.WriteAllText(charactersExportFolder + modelName + "_mesh.obj",
                model.MainMesh.Def.GetSkeletonMeshExport());*/

            foreach (var mesh in model.Meshes)
            {
                if (mesh.Def.Name == model.MainMesh.Def.Name)
                {
                    continue;
                }

                string actorName;
                string meshName;
                int skinId;

                ExplodeMeshName(mesh.Def.Name, out actorName, out meshName, out skinId);

                string realSkinId = skinId >= 10 ? skinId.ToString() : "0" + skinId;


                string exportPath = charactersExportFolder + actorName + "_" + meshName + "_" + realSkinId +
                                    "_mesh.txt";

                if (exportPath.Contains("BAM_HE_02"))
                {
                }

                string exportMesh = mesh.Def.GetIntermediateMeshExport(skinId, model.NewSkins);
                File.WriteAllText(exportPath, exportMesh);
                /*File.WriteAllText(charactersExportFolder + actorName + "_" + meshName + "_" + realSkinId + "_mesh.obj",
                    mesh.Def.GetSkeletonMeshExport());*/
            }

            //var textureList = gorillaModel.MainMesh.Def.MaterialList.GetMaterialListExport(_settings);
            //File.WriteAllText("gor.mtl", textureList);
        }

        List<MeshReference> GetAllMeshesForModel(ModelReference model)
        {
            List<MeshReference> meshes = new List<MeshReference>();

            if (model == null)
            {
                return meshes;
            }

            // TODO: Set this up to work with all types of fragments
            foreach (SkeletonHierarchyReference skeletonReference in model.SkeletonReferences)
            {
                // Should be meshes in here
                foreach (MeshReference meshReference in skeletonReference.SkeletonHierarchy.Meshes)
                {
                    meshes.Add(meshReference);
                }

                var tree = skeletonReference.SkeletonHierarchy.Tree;

                foreach (var node in tree)
                {
                    if (node.MeshReference != null)
                    {
                        meshes.Add(node.MeshReference);
                    }
                }
            }

            return meshes;
        }

        private void ExportCharacterSkeleton(string modelName)
        {
            if (_wldToInject != null)
            {
                return;
            }

            if (!Models.ContainsKey(modelName))
            {
                return;
            }

            CharacterModel characterModel = Models[modelName];

            if (characterModel.Skeleton == null)
            {
                return;
            }

            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder + "Skeletons/";
            Directory.CreateDirectory(charactersExportFolder);
            
            StringBuilder skeletonExport = new StringBuilder();

            skeletonExport.AppendLine("# Lantern Skeleton Test Export");
            skeletonExport.AppendLine("# Total verts: " + characterModel.MainMesh.Def.Vertices.Count);
            skeletonExport.AppendLine($"radius,{characterModel.Skeleton._boundingRadius}");

            foreach (var mesh in characterModel.Meshes)
            {
                if (mesh.Def == characterModel.MainMesh.Def)
                {
                    continue;
                }

                string actorName;
                string meshName;
                int partId;

                ExplodeMeshName(mesh.Def.Name, out actorName, out meshName, out partId);
                //skeletonExport.AppendLine("MAP," + actorName + meshName);
            }

            for (var i = 0; i < characterModel.Skeleton._tree.Count; i++)
            {
                var node = characterModel.Skeleton._tree[i];
                skeletonExport.Append(node.Name);

                if (node.Children.Count != 0)
                {
                    skeletonExport.Append(",");
                }

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

            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder + "Animations/";
            Directory.CreateDirectory(charactersExportFolder);

            CharacterModel characterModel = Models[modelName];

            foreach (Animation animation in characterModel.Animations.Values)
            {
                string exportString = GetAnimationString(animation, characterModel.Skeleton);
                File.WriteAllText(charactersExportFolder + characterModel.AnimationBase + "_" + animation.Name + ".txt",
                    exportString);
            }
        }

        private string GetAnimationString(Animation animation, Skeleton skeleton)
        {
            StringBuilder skeletonExport = new StringBuilder();

            skeletonExport.AppendLine("# Lantern Animation Export: " + animation.Name);
            skeletonExport.AppendLine("framecount," + animation._frameCount);

            for (var i = 0; i < skeleton._tree.Count; ++i)
            {
                for (var j = 0; j < animation._frameCount; j++)
                {
                    int frameIndex = j;

                    // Strange issue
                    if (i >= animation._boneTracks.Count)
                    {
                        _logger.LogError("HUGE ERROR");
                        break;
                    }

                    if (animation._boneTracks[i]._frames.Count == 1)
                    {
                        frameIndex = 0;
                    }

                    var node = skeleton._tree[i];

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

            return skeletonExport.ToString();
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
    }
}