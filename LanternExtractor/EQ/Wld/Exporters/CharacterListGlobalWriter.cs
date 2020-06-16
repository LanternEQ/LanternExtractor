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

            if (model.SkeletonReference == null || model.SkeletonReference.SkeletonHierarchy.AdditionalMeshes.Count == 0)
            {
                characterModel.AppendLine();
                return;
            }

            characterModel.Append(",");

            string additionalModels = string.Empty;

            foreach (var additionalModel in model.SkeletonReference.SkeletonHierarchy.AdditionalMeshes)
            {
                additionalModels += FragmentNameCleaner.CleanName(additionalModel);

                if (additionalModel != model.SkeletonReference.SkeletonHierarchy.AdditionalMeshes.Last())
                {
                    additionalModels += ";";
                }
            }

            characterModel.Append(additionalModels);
            
            if (!_characters.Contains(characterModel.ToString()))
            {
                _characters.Add(characterModel.ToString());
            }
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