using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
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
                
                AnimationModelLink[line[2]] = line[4];
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
            base.ExportData();

            FindAllAnimationsNew();
            ExportAllAnimations2();
        }

        private void FindAllAnimationsNew()
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

                    string modelName = track.ModelName;
                    string alternateModel = GetAnimationModelLink(modelName);

                    if (modelName != modelBase && alternateModel != modelBase)
                    {
                        continue;
                    }

                    skeleton.AddTrackData(track);
                }
            }
        }

        private void ExportAllAnimations2()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.SkeletonHierarchy))
            {
                return;
            }

            string path = GetExportFolderForWldType() + "Animations/";
            Directory.CreateDirectory(path);
            
            AnimationWriter2 animationWriter = new AnimationWriter2();

            foreach (var skeletonFragment in _fragmentTypeDictionary[FragmentType.SkeletonHierarchy])
            {
                SkeletonHierarchy skeleton = skeletonFragment as SkeletonHierarchy;

                if (skeleton == null)
                {
                    continue;
                }

                foreach (var animation in skeleton._animations)
                {
                    animationWriter.SetTargetAnimation(animation.Key);
                    animationWriter.AddFragmentData(skeleton);
                    animationWriter.WriteAssetToFile(path + skeleton.ModelBase + "_" + animation.Key + ".txt");
                    animationWriter.ClearExportData();
                }
            }
        }
    }
}