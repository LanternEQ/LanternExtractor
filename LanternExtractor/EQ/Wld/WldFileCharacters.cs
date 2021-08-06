using System;
using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Pfs;
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
            string filename = "ClientData/animationsources.txt";
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

        protected override void ProcessData()
        {
            base.ProcessData();
            FindAdditionalAnimationsAndMeshes();
            BuildSlotMapping();
            FindMaterialVariants();

            if (_settings.ExportCharactersToSingleFolder)
            {
                var characterFixer = new CharacterFixer();
                characterFixer.Fix(this);
            }
            
            foreach (var skeleton in GetFragmentsOfType<SkeletonHierarchy>())
            {
                skeleton.BuildSkeletonData(_wldType == WldType.Characters);
            }
        }
        
        private void BuildSlotMapping()
        {
            var materialLists = GetFragmentsOfType<MaterialList>();

            foreach (var list in materialLists)
            {
                list.BuildSlotMapping(_logger);
            }
        }

        private void FindMaterialVariants()
        {
            var materialLists = GetFragmentsOfType<MaterialList>();

            foreach (var list in materialLists)
            {
                string materialListModelName = FragmentNameCleaner.CleanName(list);

                foreach (var material in GetFragmentsOfType<Material>())
                {
                    if (material.IsHandled)
                    {
                        continue;
                    }

                    string materialName = FragmentNameCleaner.CleanName(material);

                    if (materialName.StartsWith(materialListModelName))
                    {
                        list.AddVariant(material, _logger);
                    }
                }
            }

            foreach (var material in GetFragmentsOfType<Material>())
            {
                if (material.IsHandled)
                {
                    continue;
                }
                
                _logger.LogWarning("WldFileCharacters: Material not assigned: " + material.Name);
            }
        }

        private void FindAdditionalAnimationsAndMeshes()
        {
            if (GetFragmentsOfType<TrackFragment>().Count == 0)
            {
                return;
            }

            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();

            if (skeletons.Count == 0)
            {
                if (_wldToInject == null)
                {
                    return;
                }
                
                skeletons = _wldToInject.GetFragmentsOfType<SkeletonHierarchy>();
            }
            
            if (skeletons.Count == 0)
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
                foreach (var track in GetFragmentsOfType<TrackFragment>())
                {
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
                if(GetFragmentsOfType<Mesh>().Count != 0)
                {
                    foreach (var mesh in GetFragmentsOfType<Mesh>())
                    {
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

            foreach (var track in GetFragmentsOfType<TrackFragment>())
            {
                if (track.IsPoseAnimation || track.IsProcessed)
                {
                    continue;
                }

                _logger.LogWarning("WldFileCharacters: Track not assigned: " + track.Name);
            }
        }
    }
}