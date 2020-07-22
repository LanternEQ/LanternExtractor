using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileCharacters : WldFile
    {
        public Dictionary<string, string> AnimationSources = new Dictionary<string, string>();

        public Dictionary<string, string> FilenameChanges = new Dictionary<string, string>();
        
        public WldFileCharacters(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
            ParseAnimationSources();
        }

        private void ParseAnimationSources()
        {
            string filename = "animationsources.txt";
            if (!File.Exists(filename))
            {
                _logger.LogError("WldFileCharacters: No animationsources.txt file found.");
                return;
            }
            
            string fileText = File.ReadAllText(filename);
            List<List<string>> parsedText = TextParser.ParseTextByDelimitedLines(fileText, ',', '#');

            foreach (var line in parsedText)
            {
                if (line.Count != 2)
                {
                    continue;
                }
                
                AnimationSources[line[0].ToLower()] = line[1].ToLower();
            }        
        }
        
        private string GetAnimationModelLink(string modelName)
        {
            return !AnimationSources.ContainsKey(modelName) ? modelName : AnimationSources[modelName];
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportData()
        {
            FindAdditionalAnimationsAndMeshes();
            BuildSlotMapping();
            FindMaterialVariants();

            if (_settings.ExportAllCharacterToSingleFolder)
            {
                PostProcessGlobal();
            }
            
            base.ExportData();
            ExportMeshList();
            ExportAnimationList();
            ExportCharacterList();
        }

        private void PostProcessGlobal()
        {
            FixShipNames();
            FixGolemElemental();
            FixDemiLich();
            FixAkanonKingCrown();
            FixKaladimKingCrown();
            FixFayDrake();
            FixTurtleTextures();
            FixBlackAndWhiteDragon();

            if (_fragmentNameDictionary.ContainsKey("SED_ACTORDEF"))
            {
                string zoneName = _zoneName;
            }
        }

        private void FixTurtleTextures()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                Actor actor = actorFragment as Actor;

                if (actor == null)
                {
                    continue;
                }

                if (!actor.Name.StartsWith("STU"))
                {
                    continue;
                }
                
                var materialList = actor.SkeletonReference.SkeletonHierarchy.Meshes.First().MaterialList;

                materialList.Name = materialList.Name.Replace("SEA", "STU");

                foreach (var material in materialList.Materials)
                {
                    material.Name = material.Name.Replace("SEA", "STU");
                    var bitmapNames = material.GetAllBitmapNames();
                    
                    for (int i = 0; i < bitmapNames.Count; ++i)
                    {
                        string originalName = bitmapNames[i];
                        string newName = originalName.Replace("sea", "stu");
                        material.SetBitmapName(i, newName);
                        FilenameChanges[originalName] = newName;
                    }
                }
            }
        }

        private void FixFayDrake()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                Actor actor = actorFragment as Actor;

                if (actor == null)
                {
                    continue;
                }

                if (!actor.Name.StartsWith("FDR"))
                {
                    continue;
                }

                if (actor.SkeletonReference.SkeletonHierarchy.Meshes.Count != 2)
                {
                    continue;
                }

                // Rename actor
                actor.Name = actor.Name.Replace("FDR", "FDF");
                
                // Rename skeleton reference
                var skeletonRef = actor.SkeletonReference;
                skeletonRef.Name = skeletonRef.Name.Replace("FDR", "FDF");

                // Rename skeleton
                var skeleton = actor.SkeletonReference.SkeletonHierarchy;
                skeleton.Name = skeleton.Name.Replace("FDR", "FDF");

                skeleton.ModelBase = "fdf";
                
                // Rename all main meshes
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.Meshes)
                {
                    mesh.Name = mesh.Name.Replace("FDR", "FDF");
                }
                
                // Rename all secondary meshes
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.HelmMeshes)
                {
                    mesh.Name = mesh.Name.Replace("FDR", "FDF");
                }

                // Rename all materials
                var materialList = actor.SkeletonReference.SkeletonHierarchy.Meshes.First().MaterialList;

                materialList.Name = materialList.Name.Replace("FDR", "FDF");

                foreach (var material in materialList.Materials)
                {
                    material.Name = material.Name.Replace("FDR", "FDF");

                    var bitmapNames = material.GetAllBitmapNames();
                    
                    for (int i = 0; i < bitmapNames.Count; ++i)
                    {
                        string originalName = bitmapNames[i];
                        string newName = originalName.Replace("fdr", "fdf");
                        material.SetBitmapName(i, newName);
                        FilenameChanges[originalName] = newName;
                    }
                }
            }
        }

        private void FixKaladimKingCrown()
        {
           if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
           {
               return;
           }

           if (!_fragmentNameDictionary.ContainsKey("KAHE0001_MDF"))
           {
               return;
           }

           Material crownMaterial = _fragmentNameDictionary["KAHE0001_MDF"] as Material;

           if (crownMaterial == null)
           {
               return;
           }

           crownMaterial.ShaderType = ShaderType.TransparentMasked;
        }

        private void FixAkanonKingCrown()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            if (!_fragmentNameDictionary.ContainsKey("CLHE0004_MDF"))
            {
                return;
            }

            Material crownMaterial = _fragmentNameDictionary["CLHE0004_MDF"] as Material;

            if (crownMaterial == null)
            {
                return;
            }

            crownMaterial.ShaderType = ShaderType.TransparentMasked;
        }

        private void FixDemiLich()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                Actor actor = actorFragment as Actor;

                if (actor == null)
                {
                    continue;
                }

                if (actor.SkeletonReference == null)
                {
                    continue;
                }
                
                if (!actor.Name.StartsWith("SDE"))
                {
                    continue;
                }
                
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.Meshes)
                {
                    foreach (var material in mesh.MaterialList.Materials)
                    {
                        // This texture needs to be masked
                        if (material.Name == "SDEUA0006_MDF")
                        {
                            material.ShaderType = ShaderType.TransparentMasked;
                        }
                        
                        var bitmapNames = material.GetAllBitmapNames();
                        
                        for (var i = 0; i < bitmapNames.Count; i++)
                        {
                            if (!bitmapNames[i].StartsWith("dml"))
                            {
                                continue;
                            }

                            string originalName = bitmapNames[i];
                            string newName = originalName.Replace("dml", "sde");
                            material.SetBitmapName(i, newName);
                            FilenameChanges[originalName] = newName;
                        }
                    }
                }
            }
        }

        private void FixGolemElemental()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                Actor actor = actorFragment as Actor;

                if (actor == null)
                {
                    continue;
                }

                if (!actor.Name.StartsWith("GOM"))
                {
                    continue;
                }
                
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.Meshes)
                {
                    foreach (var material in mesh.MaterialList.Materials)
                    {
                        material.Name = material.Name.Replace("GOL", "GOM");

                        var bitmapNames = material.GetAllBitmapNames();

                        for (var i = 0; i < bitmapNames.Count; i++)
                        {
                            string originalName = bitmapNames[i];
                            string newName = originalName.Replace("gol", "gom");
                            material.SetBitmapName(i, newName);
                            FilenameChanges[originalName] = newName;
                        }
                    }
                }
            }
        }

        private void FixShipNames()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                Actor actor = actorFragment as Actor;

                if (actor == null)
                {
                    continue;
                }

                if (actor.Name.StartsWith("GSP"))
                {
                    actor.MeshReference.Mesh.Name = actor.MeshReference.Mesh.Name.Replace("GHOSTSHIP", "GSP");
                    actor.MeshReference.Mesh.MaterialList.Name =
                        actor.MeshReference.Mesh.MaterialList.Name.Replace("GHOSTSHIP", "GSP");
                }

                if (actor.Name.StartsWith("LAUNCH"))
                {
                    actor.Name = actor.MeshReference.Mesh.Name.Replace("DMSPRITEDEF", "ACTORDEF");
                }

                if (actor.Name.StartsWith("PRE"))
                {
                    if (actor.SkeletonReference == null)
                    {
                        continue;
                    }

                    switch (actor.SkeletonReference.SkeletonHierarchy.Name)
                    {
                        // Bloated Belly in Iceclad
                        case "OGS_HS_DEF":
                        {
                            actor.Name = actor.Name.Replace("PRE", "OGS");
                            break;
                        }
                        // Sea King, Golden Maiden, StormBreaker, SirensBane
                        case "PRE_HS_DEF":
                        {
                            break;
                        }
                        default:
                            throw new NotImplementedException();
                            break;
                    }
                }


                if (actor.Name.StartsWith("SHIP"))
                {
                    if (actor.SkeletonReference == null)
                    {
                        continue;
                    }

                    switch (actor.SkeletonReference.SkeletonHierarchy.Name)
                    {
                        // Icebreaker in Iceclad
                        case "GNS_HS_DEF":
                        {
                            actor.Name = actor.Name.Replace("SHIP", "GNS");
                            break;
                        }
                        // Maidens Voyage in Firiona Vie
                        case "ELS_HS_DEF":
                        {
                            actor.Name = actor.Name.Replace("SHIP", "ELS");
                            break;
                        }
                        default:
                            throw new NotImplementedException();
                            break;
                    }
                }
            }
        }
        
        private void FixBlackAndWhiteDragon()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                Actor actor = actorFragment as Actor;

                if (actor == null)
                {
                    continue;
                }

                if (!actor.Name.StartsWith("BWD"))
                {
                    continue;
                }
                
                if (_fragmentNameDictionary.ContainsKey("BWDCH0101_MDF"))
                {
                    Material material = _fragmentNameDictionary["BWDCH0101_MDF"] as Material;
                    if (material != null)
                    {
                        material.ShaderType = ShaderType.Diffuse;
                    }
                }

                if (_fragmentNameDictionary.ContainsKey("BWD_MP"))
                {
                    MaterialList bwdMaterialList = _fragmentNameDictionary["BWD_MP"] as MaterialList;

                    if (bwdMaterialList != null)
                    {
                        var slot = bwdMaterialList.Slots["bwd_ch01"];
                        slot[1] = "d_bwdch0101";
                    }
                }
            }
        }

        private void BuildSlotMapping()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.MaterialList))
            {
                return;
            }

            foreach (var meshListFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                MaterialList materialList = meshListFragment as MaterialList;

                if (materialList == null)
                {
                    continue;
                }
                
                materialList.BuildSlotMapping(_logger);
            }
        }

        private void FindMaterialVariants()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.MaterialList))
            {
                return;
            }
            
            foreach (var materialListFragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                MaterialList materialList = materialListFragment as MaterialList;

                string materialListModelName = FragmentNameCleaner.CleanName(materialList);
                
                if (materialList == null)
                {
                    continue;
                }

                foreach (var materialFragment in _fragmentTypeDictionary[FragmentType.Material])
                {
                    Material material = materialFragment as Material;

                    if (material == null)
                    {
                        continue;
                    }

                    if (material.IsHandled)
                    {
                        continue;
                    }

                    string materialName = FragmentNameCleaner.CleanName(material);

                    if (materialName.StartsWith(materialListModelName))
                    {
                        materialList.AddVariant(material, _logger);
                    }
                }
            }

            // Check for debugging
            foreach (var materialFragment in _fragmentTypeDictionary[FragmentType.Material])
            {
                Material material = materialFragment as Material;

                if (material == null)
                {
                    continue;
                }

                if (material.IsHandled)
                {
                    continue;
                }
                
                _logger.LogWarning("WldFileCharacters: Material not assigned: " + material.Name);
            }
        }

        private void ExportCharacterList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Actor))
            {
                return;
            }

            TextAssetWriter characterListWriter = null;

            if (_settings.ExportAllCharacterToSingleFolder)
            {
                characterListWriter = new CharacterListGlobalWriter(_fragmentTypeDictionary[FragmentType.Actor].Count);
            }
            else
            {
                characterListWriter = new CharacterListWriter(_fragmentTypeDictionary[FragmentType.Actor].Count);
            }
            
            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.Actor])
            {
                characterListWriter.AddFragmentData(actorFragment);
            }

            if (_settings.ExportAllCharacterToSingleFolder)
            {
                characterListWriter.WriteAssetToFile("all/characters.txt");
            }
            else
            {
                characterListWriter.WriteAssetToFile(GetRootExportFolder() + "characters.txt");
            }
        }

        private void ExportAnimationList()
        {
            var skeletons = GetFragmentsOfType(FragmentType.SkeletonHierarchy);

            if (skeletons == null)
            {
                if (_wldToInject == null)
                {
                    return;
                }

                skeletons = _wldToInject.GetFragmentsOfType(FragmentType.SkeletonHierarchy);

                if (skeletons == null)
                {
                    return;
                }
            }

            TextAssetWriter animationWriter = null;

            if (!_settings.ExportAllCharacterToSingleFolder)
            {
                animationWriter = new CharacterAnimationListWriter();
            }
            else
            {
                animationWriter = new CharacterAnimationGlobalListWriter();
            }

            foreach (var skeletonFragment in skeletons)
            {
                animationWriter.AddFragmentData(skeletonFragment);
            }
            
            animationWriter.WriteAssetToFile(GetRootExportFolder() + "character_animations.txt");
        }

        private void FindAdditionalAnimationsAndMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.TrackFragment))
            {
                return;
            }

            var skeletons = GetFragmentsOfType(FragmentType.SkeletonHierarchy);

            if (skeletons == null)
            {
                if (_wldToInject == null)
                {
                    return;
                }
                
                skeletons = _wldToInject.GetFragmentsOfType(FragmentType.SkeletonHierarchy);
            }

            if (skeletons == null)
            {
                return;
            }

            foreach (var skeletonFragment in skeletons)
            {
                SkeletonHierarchy skeleton = skeletonFragment as SkeletonHierarchy;

                if (skeleton == null)
                {
                    continue;
                }

                string modelBase = skeleton.ModelBase;
                string alternateModel = GetAnimationModelLink(modelBase);
                
                // TODO: Alternate model bases
                foreach (var trackFragment in _fragmentTypeDictionary[FragmentType.TrackFragment])
                {
                    TrackFragment track = trackFragment as TrackFragment;

                    if (track == null)
                    {
                        continue;
                    }

                    if (track.IsPoseAnimation)
                    {
                        continue;
                    }

                    if (!track.IsNameParsed)
                    {
                        track.ParseTrackData(_logger);
                    }
                    
                    string trackModelBase = track.ModelName;
                    
                    if (trackModelBase != modelBase && alternateModel != trackModelBase)
                    {
                        continue;
                    }

                    skeleton.AddTrackData(track);
                }
                
                // TODO: Split to another function
                if(_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
                {
                    foreach (var meshReferenceFragment in _fragmentTypeDictionary[FragmentType.Mesh])
                    {
                        Mesh mesh = meshReferenceFragment as Mesh;

                        if (mesh == null)
                        {
                            continue;
                        }

                        if (mesh.IsHandled)
                        {
                            continue;
                        }

                        string cleanedName = FragmentNameCleaner.CleanName(mesh);

                        string basename = cleanedName;

                        bool endsWithNumber = char.IsDigit(cleanedName[cleanedName.Length - 1]);
                    
                        if (endsWithNumber)
                        {
                            int id = Convert.ToInt32(cleanedName.Substring(cleanedName.Length - 2));
                            cleanedName = cleanedName.Substring(0, cleanedName.Length - 2);

                            if (cleanedName.Length != 3)
                            {
                                string modelType = cleanedName.Substring(cleanedName.Length - 3);
                                cleanedName = cleanedName.Substring(0, cleanedName.Length - 2);
                            }

                            basename = cleanedName;
                        }
                    
                        if (basename == modelBase)
                        {
                            skeleton.AddAdditionalMesh(mesh);
                        }
                    }
                }
            }

            foreach (var trackFragment in _fragmentTypeDictionary[FragmentType.TrackFragment])
            {
                TrackFragment track = trackFragment as TrackFragment;

                if (track == null)
                {
                    continue;
                }
                
                if (track.IsPoseAnimation || track.IsProcessed)
                {
                    continue;
                }

                _logger.LogWarning("WldFileCharacters: Track not assigned: " + track.Name);
            }
        }
    }
}