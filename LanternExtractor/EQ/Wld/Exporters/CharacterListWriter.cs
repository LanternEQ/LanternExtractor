using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class CharacterListWriter : TextAssetWriter
    {
        public CharacterListWriter(int modelCount)
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Character List");
            _export.AppendLine("# Total models: " + modelCount);
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            Actor model = data as Actor;
            
            if (model == null)
            {
                return;
            }
            
            _export.Append(FragmentNameCleaner.CleanName(model));

            if (model.SkeletonReference.SkeletonHierarchy.AdditionalMeshes.Count == 0)
            {
                _export.AppendLine();
                return;
            }

            _export.Append(",");

            string additionalModels = string.Empty;

            foreach (var additionalModel in model.SkeletonReference.SkeletonHierarchy.AdditionalMeshes)
            {
                additionalModels += FragmentNameCleaner.CleanName(additionalModel);

                if (additionalModel != model.SkeletonReference.SkeletonHierarchy.AdditionalMeshes.Last())
                {
                    additionalModels += ";";
                }
            }

            _export.Append(additionalModels);
            _export.AppendLine();
        }
    }
}