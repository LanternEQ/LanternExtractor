using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class CharacterListGlobalWriter : TextAssetWriter
    {
        private List<string> _characters = new List<string>();

        public CharacterListGlobalWriter(int modelCount)
        {
            if (!File.Exists("all/characters.txt"))
            {
                return;
            }
            
            var text = File.ReadAllLines("all/characters.txt");

            foreach (var line in text)
            {
                _characters.Add(line);
            }
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            Actor model = data as Actor;
            
            if (model == null)
            {
                return;
            }

            StringBuilder characterModel = new StringBuilder();
            
            characterModel.Append(FragmentNameCleaner.CleanName(model));

            if (model.SkeletonReference == null)
            {
                AddCharacterIfNotDuplicate(characterModel.ToString());
                return;
            }
            
            characterModel.Append(",");
            
            List<string> mainMeshes = new List<string>();

            foreach (var mesh in model.SkeletonReference.SkeletonHierarchy.Meshes)
            {
                mainMeshes.Add(FragmentNameCleaner.CleanName(mesh));
            }

            mainMeshes.Sort();
            string mainMeshesString = string.Empty;
            
            foreach (var mesh in mainMeshes)
            {
                mainMeshesString += mesh;
                
                if (mesh != mainMeshes.Last())
                {
                    mainMeshesString += ";";
                }
            }
            
            characterModel.Append(mainMeshesString);

            if (model.SkeletonReference.SkeletonHierarchy.HelmMeshes.Count == 0)
            {
                AddCharacterIfNotDuplicate(characterModel.ToString());
                return;
            }
            
            characterModel.Append(",");

            List<string> additionalModels = new List<string>();

            foreach (var additionalModel in model.SkeletonReference.SkeletonHierarchy.HelmMeshes)
            {
                additionalModels.Add(FragmentNameCleaner.CleanName(additionalModel));
            }

            additionalModels.Sort();
            string additionalModelsString = string.Empty;
            
            foreach (var additionalModel in additionalModels)
            {
                additionalModelsString += additionalModel;
                
                if (additionalModel != additionalModels.Last())
                {
                    additionalModelsString += ";";
                }
            }
            
            characterModel.Append(additionalModelsString);
            
            AddCharacterIfNotDuplicate(characterModel.ToString());
        }

        private void AddCharacterIfNotDuplicate(string character)
        {
            string characterBase = character.Split(',')[0];

            for (int i = 0; i < _characters.Count; ++i)
            {
                if (_characters[i].StartsWith(characterBase + ","))
                {
                    if (_characters[i].Length >= character.Length)
                    {
                        return;
                    }
                    
                    _characters.RemoveAt(i);
                    break;
                }
            }
            
            _characters.Add(character);
        }

        public override void WriteAssetToFile(string fileName)
        {
            _characters.Sort();
            
            foreach (var character in _characters)
            {
                _export.AppendLine(character);
            }
            
            base.WriteAssetToFile(fileName);
        }
    }
}