using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class TextureListWriter : TextAssetWriter
    {
        public TextureListWriter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Texture List Information");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                                          "BitmapName, IsMasked");        
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

                foreach (var bitmap in material.BitmapInfoReference.BitmapInfo.BitmapNames)
                {
                    string bitmapName = bitmap.Filename.Substring(0, bitmap.Filename.Length - 4);
                    _export.Append(bitmapName);
                    _export.Append(",");
                    _export.Append(material.ShaderType == ShaderType.TransparentMasked ? 1 : 0);
                    _export.AppendLine();
                }
            }        
        }
    }
}