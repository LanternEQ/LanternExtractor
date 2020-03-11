using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshIntermediateMaterialsExport : TextAssetExporter
    {
        private Settings _settings;
        private string _modelName;
        
        public MeshIntermediateMaterialsExport(Settings settings, string modelName)
        {
            _settings = settings;
            _modelName = modelName;
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            MaterialList list = data as MaterialList;

            if (list == null)
            {
                return;
            }
            
            for (var i = 0; i < list.Materials.Count; i++)
            {
                Material material = list.Materials[i];

                if (string.IsNullOrEmpty(material.GetFirstBitmapExportFilename()))
                {
                    continue;
                }

                string materialName = MaterialList.GetMaterialPrefix(material.ShaderType) + material.GetFirstBitmapNameWithoutExtension();
                
                _export.Append(i);
                _export.Append(",");
                _export.Append(materialName);
                _export.AppendLine();
            }
        }
    }
}