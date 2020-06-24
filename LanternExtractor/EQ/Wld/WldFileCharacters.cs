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
        public Dictionary<string, string> AnimationModelLink;
        
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
                
                AnimationModelLink[line[2].ToLower()] = line[4].ToLower();
            }        
        }
        
        private string GetAnimationModelLink(string modelName)
        {
            return !AnimationModelLink.ContainsKey(modelName) ? modelName : AnimationModelLink[modelName];
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportData()
        {
            FindAdditionalAnimationsAndMeshes();
            BuildSlotMapping();
            FindMaterialVariants();
            base.ExportData();
            ExportMeshList();
            ExportAnimationList();
            ExportCharacterList();
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
                    
                    if (material.Name == "BEAHE0102_MDF")
                    {
                        
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

                    string modelName = track.ModelName;
                    string alternateModel = GetAnimationModelLink(modelBase);
                    
                    if (modelName != modelBase && alternateModel != modelName)
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
                    if (cleanedName.StartsWith(modelBase))
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