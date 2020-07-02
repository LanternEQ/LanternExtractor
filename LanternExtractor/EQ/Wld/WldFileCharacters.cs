using System;
using System.Collections.Generic;
using System.IO;
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

                if (actor.Name.StartsWith("PRE"))
                {
                    if (actor.SkeletonReference == null)
                    {
                        continue;
                    }

                    switch (actor.SkeletonReference.SkeletonHierarchy.Name)
                    {
                        // Icebreaker in Iceclad
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
                
                _logger.LogError("Material not assigned: " + material.Name);
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
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.SkeletonHierarchy))
            {
                return;
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

            foreach (var skeletonFragment in _fragmentTypeDictionary[FragmentType.SkeletonHierarchy])
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
            
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.SkeletonHierarchy))
            {
                return;
            }

            foreach (var skeletonFragment in _fragmentTypeDictionary[FragmentType.SkeletonHierarchy])
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

                    track.ParseTrackData(_logger);
                    
                    string trackModelBase = track.ModelName;




                    if (trackModelBase != modelBase && alternateModel != trackModelBase)
                    {
                        continue;
                    }

                    skeleton.AddTrackData(track);
                }
                
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
                
                _logger.LogError("Track not assigned: " + track.Name);
            }
        }
    }
}