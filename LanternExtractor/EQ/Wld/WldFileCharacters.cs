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
            base.ExportData();
            ExportMeshList();
            ExportAnimationList();
            ExportCharacterList();
        }

        private void ExportCharacterList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.ModelReference))
            {
                return;
            }
            
            CharacterListWriter characterListWriter = new CharacterListWriter(_fragmentTypeDictionary[FragmentType.ModelReference].Count);

            foreach (var actorFragment in _fragmentTypeDictionary[FragmentType.ModelReference])
            {
                characterListWriter.AddFragmentData(actorFragment);
            }
            
            characterListWriter.WriteAssetToFile(GetRootExportFolder() + "characters.txt");
        }

        private void ExportAnimationList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.SkeletonHierarchy))
            {
                return;
            }
            
            CharacterAnimationListWriter animationListWriter = new CharacterAnimationListWriter();

            foreach (var skeletonFragment in _fragmentTypeDictionary[FragmentType.SkeletonHierarchy])
            {
                animationListWriter.AddFragmentData(skeletonFragment);
            }
            
            animationListWriter.WriteAssetToFile(GetRootExportFolder() + "character_animations.txt");
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

                    if (track.IsProcessed)
                    {
                        continue;
                    }

                    track.ParseTrackData();

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
                    else
                    {
                        _logger.LogError("Unable to assign additional mesh: " + cleanedName);
                    }
                }
            }
        }
    }
}