using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshIntermediateMaterialsExport : TextAssetWriter
    {
        public override void AddFragmentData(WldFragment data)
        {
            if (!(data is MaterialList list))
            {
                return;
            }
            
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Material List Intermediate Format");
            _export.AppendLine(LanternStrings.ExportHeaderFormat + "Index, MaterialName, AnimationTextures, AnimationDelayMs, SkinTextures");

            for (int i = 0; i < list.Materials.Count; i++)
            {
                Material material = list.Materials[i];

                string materialName = material.GetFullMaterialName();

                _export.Append(i);
                _export.Append(",");
                _export.Append(materialName);
                _export.Append(",");

                var bitmapNames = list.Materials[i].GetAllBitmapNames();

                if (bitmapNames.Count != 0)
                {
                    for (int j = 0; j < bitmapNames.Count; j++)
                    {
                        if (j != 0)
                        {
                            _export.Append(";");
                        }

                        _export.Append(bitmapNames[j]);
                    }
                }

                _export.Append(",");
                _export.Append(material.BitmapInfoReference?.BitmapInfo.BitmapNames.Count ?? 0);
                
                _export.Append(",");
                _export.Append(material.BitmapInfoReference?.BitmapInfo.AnimationDelayMs ?? 0);

                _export.Append(",");

                var skinVariants = list.GetMaterialVariants(material, new EmptyLogger());

                if (skinVariants.Count != 0)
                {
                    for (int j = 0; j < skinVariants.Count; j++)
                    {
                        if (j != 0)
                        {
                            _export.Append(";");
                        }

                        _export.Append(skinVariants[j]);
                    }
                }
                
                _export.AppendLine();
            }
        }
    }
}