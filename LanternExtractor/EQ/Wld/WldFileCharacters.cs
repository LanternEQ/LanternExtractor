using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
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
        
        public WldFileCharacters(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings) : base(wldFile, zoneName, type, logger, settings)
        {
            AnimationModelLink = new Dictionary<string, string>();

            var fileText = File.ReadAllText("models.txt");

            var parsedText = TextParser.ParseTextByDelimitedLines(fileText, ',', '#');

            foreach (var line in parsedText)
            {
                AnimationModelLink[line[2]] = line[4];
            }
        }
        
        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportWldData()
        {
            ImportCharacters();
            ImportCharacterPalettes();
            ImportSkeletons();
            ResolveAnimations();

            foreach (var model in Models)
            {
                var animationBase = model.Key;
                CreateSkeletonPieceHierarchy(animationBase);
                FindAnimations(animationBase);
                ExportCharacterMesh(animationBase);
                ExportCharacterSkeleton(animationBase);
                //ExportCharacterAnimations(animationBase);
            }

            FindAllAnimations();
            ExportAllAnimations();
            ExportModelList();
        }

        private void ExportAllAnimations()
        {
            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder + "Animation/";
            Directory.CreateDirectory(charactersExportFolder);
            
            string lastAnimationModel = string.Empty;
            Skeleton skeleton = null;
            
            foreach (var animation in _animations)
            {
                string modelName = animation.Key.Substring(0, 3);

                if (animation.Key.Contains("ELF"))
                {
                    
                }
                
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
                    _logger.LogError("Unable to export animation because there was no associated skeleton: " + animation.Key);
                    continue;
                }
                
                string exportString = GetAnimationString(animation.Value, skeleton);
                File.WriteAllText(charactersExportFolder + animation.Key + ".txt", exportString);
            }
        }

        private void ExportModelList()
        {
            StringBuilder exportListBuilder = new StringBuilder();
            exportListBuilder.AppendLine("# Model list: " + _zoneName);
            exportListBuilder.AppendLine("# Total models: " + Models.Count);

            foreach (var model in Models)
            {
                exportListBuilder.AppendLine(model.Value.ModelBase);
            }
            
            string zoneExportFolder = _zoneName + "/";
            
            // create directory
            File.WriteAllText(zoneExportFolder + _zoneName + "_characters.txt", exportListBuilder.ToString());
        }

        private void ImportCharacters()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x14))
            {
                return;
            }

            List<WldFragment> fragments = _fragmentTypeDictionary[0x14];

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

                FindModelAnimationBase(characterModel, actorName);
                Models[actorName] = characterModel;
            }

            // Look for alternate meshes (e.g. heads)
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

                if (!WldMaterialPalette.ExplodeName(material.Name, out charName, out skinId, out partName))
                {
                    _logger.LogError("WldFileCharacter: Error exploding material details: " + material.Name);
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
                
                _logger.LogError($"Adding {material.Name}");
                
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

        private void CreateSkeletonPieceHierarchy(string modelName)
        {
            CharacterModel model = Models[modelName];

            if (model == null)
            {
                return;
            }

            Dictionary<int, string> boneNames = new Dictionary<int, string>();
            RecurseBone(0, model.Skeleton._pose._skeleton._tree, string.Empty,string.Empty, boneNames);
        }

        private void RecurseBone(int index, List<SkeletonNode> treeNodes, string runningName, string runningIndex,
            Dictionary<int, string> paths)
        {
            SkeletonNode node = treeNodes[index];

            if (node.Name != string.Empty)
            {
                runningName += node.Name + "/";
            }
            
            if (node.Name != string.Empty)
            {
                runningIndex += node.Index + "/";
            }

            node.FullPath = runningName.Substring(0, runningName.Length - 1);
            node.FullIndexPath = runningIndex.Substring(0, runningIndex.Length - 1);

            if (node.Children.Count == 0)
            {
                return;
            }

            foreach (var childNode in node.Children)
            {
                RecurseBone(childNode, treeNodes, runningName, runningIndex, paths);
            }
        }
        
        private void FindAllAnimations()
        {
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

                string partName = track.TrackDefFragment.Name;

                string animNameCur = partName.Substring(0, 3);
                string modelNameCur = partName.Substring(3, 3);


                if (!Models.ContainsKey(modelNameCur))
                {
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
                    //model.AddAnimation(animationName, newAnimation);
                    _animations[modelNameCur + "_" + animationName] = newAnimation;
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
        }

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

            string animationBade = model.AnimationBase;

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
                    continue;
                }

                string partName = track.TrackDefFragment.Name;

                string animNameCur = partName.Substring(0, 3);
                string modelNameCur = partName.Substring(3, 3);

                if (modelNameCur == "ELF")
                {
                    
                }

                // TODO: FIX THIS SHIT
                if (modelNameCur != animationBade)
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
            
            model.AddAnimation("POS", model.Skeleton._pose);
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
                
                
                
                string exportPath = charactersExportFolder + actorName + "_" + meshName + "_" + realSkinId + "_mesh.txt";

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
            foreach (HierSpriteFragment skeletonReference in model.SkeletonReferences)
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
            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder + "Skeletons/";
            Directory.CreateDirectory(charactersExportFolder);
            
            CharacterModel characterModel = Models[modelName];

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
                File.WriteAllText(charactersExportFolder + characterModel.AnimationBase + "_" + animation.Name +".txt", exportString);
            }
        }

        private string GetAnimationString(Animation animation, Skeleton skeleton)
        {
            StringBuilder skeletonExport = new StringBuilder();

                skeletonExport.AppendLine("# Lantern Animation Test Export: " + animation.Name);
                skeletonExport.AppendLine("# Total frames: " + animation._frameCount);

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

                        skeletonExport.Append(node.FullIndexPath);
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

        bool FindModelAnimationBase(CharacterModel model, string modelName)
        {
            if (!AnimationModelLink.ContainsKey(modelName))
            {
                return false;
            }
            
            var animationBase = AnimationModelLink[modelName];
            model.SetAnimationBase(animationBase == string.Empty ? modelName : animationBase);
            return false;
        }
    }
}