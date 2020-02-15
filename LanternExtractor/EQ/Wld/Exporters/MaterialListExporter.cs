using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MaterialListExporter : TextAssetExporter
    {
        public MaterialListExporter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Material List Information");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                                          "BitmapName, BitmapCount, AnimationDelayMs (optional)");        
        }

        public override void AddFragmentData(WldFragment data)
        {
            MaterialList list = data as MaterialList;

            if (list == null)
            {
                return;
            }

            foreach (Material material in list.Materials)
            {
                if (material.ShaderType == ShaderType.Invisible)
                {
                    continue;
                }

                if (material.BitmapInfoReference == null)
                {
                    continue;
                }

                string materialPrefix = MaterialList.GetMaterialPrefix(material.ShaderType);

                string textureName = material.BitmapInfoReference.BitmapInfo.BitmapNames[0]
                    .Filename;

                textureName = textureName.Substring(0, textureName.Length - 4);
                _export.Append(materialPrefix);
                _export.Append(textureName);
                _export.Append(",");
                _export.Append(material.BitmapInfoReference.BitmapInfo.BitmapNames.Count);

                if (material.BitmapInfoReference.BitmapInfo.IsAnimated)
                {
                    _export.Append("," + material.BitmapInfoReference.BitmapInfo.AnimationDelayMs);
                }

                _export.AppendLine();
            }        
        }
    }
}