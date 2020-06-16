using System.Collections.Generic;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MaterialListGlobalWriter : TextAssetWriter
    {
        private List<string> _processedFragments = new List<string>();
        
        private List<string> _materials = new List<string>();

        public MaterialListGlobalWriter()
        {
            string filePath = "all/materials_characters.txt";
            if (!File.Exists(filePath))
            {
                return;
            }
            
            var text = File.ReadAllLines(filePath);

            foreach (var line in text)
            {
                _materials.Add(line);
            }
        }

        public override void AddFragmentData(WldFragment data)
        {
            MaterialList list = data as MaterialList;

            if (list == null)
            {
                return;
            }

            List<Material> allMaterials = new List<Material>();
            
            allMaterials.AddRange(list.Materials);
            if (list.AdditionalMaterials != null)
            {
                allMaterials.AddRange(list.AdditionalMaterials);
            }

            foreach (Material material in allMaterials)
            {
                if (material.ShaderType == ShaderType.Invisible)
                {
                    continue;
                }

                if (material.BitmapInfoReference == null)
                {
                    continue;
                }

                if (_processedFragments.Contains(material.ShaderType + material.Name))
                {
                    continue;
                }

                _processedFragments.Add(material.ShaderType + material.Name);

                string materialPrefix = MaterialList.GetMaterialPrefix(material.ShaderType);
                string materialName = materialPrefix + FragmentNameCleaner.CleanName(material);

                StringBuilder newMaterial = new StringBuilder();
                
                newMaterial.Append(materialName);
                newMaterial.Append(",");
                newMaterial.Append(material.BitmapInfoReference.BitmapInfo.BitmapNames.Count);
                newMaterial.Append(",");

                if (!material.BitmapInfoReference.BitmapInfo.IsAnimated)
                {
                    newMaterial.Append(material.GetFirstBitmapNameWithoutExtension());
                }
                else
                {
                    newMaterial.Append(material.BitmapInfoReference.BitmapInfo.AnimationDelayMs);
                    newMaterial.Append(",");

                    var allBitmapNames = material.GetAllBitmapNames();

                    for (var i = 0; i < allBitmapNames.Count; i++)
                    {
                        string bitmapName = allBitmapNames[i];
                        newMaterial.Append(bitmapName);

                        if (i < allBitmapNames.Count - 1)
                        {
                            newMaterial.Append(";");
                        }
                    }
                }

                _materials.Add(newMaterial.ToString());
            }
            
            if (list.Slots != null)
            {
                    
            }
        }
        
        public override void WriteAssetToFile(string fileName)
        {
            _materials.Sort();
            
            foreach (var material in _materials)
            {
                _export.AppendLine(material);
            }
            
            base.WriteAssetToFile(fileName);
        }
    }
}