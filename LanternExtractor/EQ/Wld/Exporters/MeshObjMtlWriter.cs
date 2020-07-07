using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshObjMtlWriter : TextAssetWriter
    {
        private Settings _settings;
        private string _modelName;
        
        public MeshObjMtlWriter(Settings settings, string modelName)
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
            
            bool createdNullMaterial = false;

            foreach (Material material in list.Materials)
            {
                string filenameWithoutExtension = material.GetFirstBitmapNameWithoutExtension();
                
                if (string.IsNullOrEmpty(filenameWithoutExtension))
                {
                    if(!createdNullMaterial)
                    {
                        _export.AppendLine(LanternStrings.ObjNewMaterialPrefix + " " + "null");
                        _export.AppendLine("Ka 1.000 1.000 1.000");
                        _export.AppendLine("Kd 1.000 1.000 1.000");
                        _export.AppendLine("Ks 0.000 0.000 0.000");
                        _export.AppendLine("d 1.0 ");
                        _export.AppendLine("illum 2");

                        createdNullMaterial = true;
                    }

                    continue;
                }

                if (material.ShaderType == ShaderType.Invisible && !_settings.ExportHiddenGeometry)
                {
                    continue;
                }

                _export.AppendLine(LanternStrings.ObjNewMaterialPrefix + " " + MaterialList.GetMaterialPrefix(material.ShaderType) + filenameWithoutExtension);
                _export.AppendLine("Ka 1.000 1.000 1.000");
                _export.AppendLine("Kd 1.000 1.000 1.000");
                _export.AppendLine("Ks 0.000 0.000 0.000");
                _export.AppendLine("d 1.0 ");
                _export.AppendLine("illum 2");
                _export.AppendLine("map_Kd " + "Textures/" + material.GetFirstBitmapExportFilename());
            }
        }
    }
}