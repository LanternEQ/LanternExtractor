using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshIntermediateMaterialsWriter : TextAssetWriter
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
                _export.Append(i);
                _export.Append(",");

                List<Material> allMaterials = new List<Material> {material};
                allMaterials.AddRange(list.GetMaterialVariants(material, null));
                
                for (int j = 0; j < allMaterials.Count; j++)
                {
                    var currentMaterial = allMaterials[j];

                    if (currentMaterial == null)
                    {
                        currentMaterial = allMaterials.First();
                    }

                    _export.Append(GetMaterialString(currentMaterial));

                    if (j < list.VariantCount)
                    {
                        _export.Append(";");
                    }
                }

                _export.Append(",");
                _export.Append(material.BitmapInfoReference?.BitmapInfo.AnimationDelayMs ?? 0);
                _export.AppendLine();
            }
        }
        
        private string GetMaterialString(Material material)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(material.GetFullMaterialName());

                var bitmapNames = material.GetAllBitmapNames();

            foreach (var bitmap in bitmapNames)
            {
                sb.Append(":");
                sb.Append(bitmap);
            }

            return sb.ToString();
        }
    }
}