using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileCharacters : WldFile
    {
        public WldFileCharacters(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings) : base(wldFile, zoneName, type, logger, settings)
        {
        }
        
        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportWldData()
        {
            ProcessCharacterData();
        }
        
        private void ProcessCharacterData()
        {
            ImportCharacters();
            ImportCharacterPalettes();
            ImportSkeletons();
            ResolveAnimations();

            foreach (var model in Models)
            {
                var animationBase = model.Value.AnimationBase;
                CreateSkeletonPieceHierarchy(animationBase);
                FindGorillaAnimation(animationBase);
                ExportCharacterMesh(animationBase);
                ExportCharacterSkeleton(animationBase);
                ExportCharacterAnimations(animationBase);
            }
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

                string actorName = fragment.Name.Replace("_ACTORDEF", "");

                List<MeshReference> meshes = GetAllMeshesForModel(modelReference);

                MeshReference mainMeshReference;
                MaterialList materialList;

                FindMainMesh(meshes, actorName, out mainMeshReference, out materialList);

                if (mainMeshReference == null)
                {
                    continue;
                }
                
                Mesh mainMeshDef = mainMeshReference.Mesh;

                // Create the main mesh
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

                if (modelNameCur == "GOR" && animNameCur == "C05" && animBase == "GOR")
                {
                    
                }
                

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

        private void ExportCharacterMesh(string modelName)
        {
            string charactersExportFolder = _zoneName + "/" + LanternStrings.ExportCharactersFolder;
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

            // gorillaModel.MainMesh.Def.ShiftSkeletonValues(gorillaModel.skeleton._tree,
            // gorillaModel.skeleton._pose._boneTracks, mat4.Identity, 0, 0, _logger);


            File.WriteAllText(charactersExportFolder + modelName + "_mesh.txt", model.MainMesh.Def.GetIntermediateMeshExport());

            
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
                /*skeletonExport.Append(",");
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
                skeletonExport.Append(characterModel.skeleton._pose._boneTracks[i]._frames[0].Rotation.EulerAngles.y);*/

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
                    if (animation._frameCount == 0)
                    {
                            
                    }
                    
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
            model.SetAnimBase(modelName);

            return false;
        }
    }
}