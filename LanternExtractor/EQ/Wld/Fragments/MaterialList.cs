using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x31 - Material List
    /// A list of material fragments (0x30) that make up a single list.
    /// This list is used in the rendering of an mesh (via the list indices).
    /// </summary>
    class MaterialList : WldFragment
    {
        /// <summary>
        /// The materials in the list
        /// </summary>
        public List<Material> Materials { get; private set; }

        /// <summary>
        /// Mapping of slot name to material list index
        /// Used for setting the correct index in the Skins mapping
        /// </summary>
        public Dictionary<string, int> IndexSlotMapping;

        /// <summary>
        /// Mapping of skin list variation to dictionary of slot to material
        /// </summary>
        public Dictionary<int, Dictionary<int, Material>> Skins;

        public List<Material> GeneralTextures;

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            // String reference
            Name = stringHash[-reader.ReadInt32()];

            Materials = new List<Material>();

            // flags
            reader.ReadInt32();

            int materialCount = reader.ReadInt32();

            for (int i = 0; i < materialCount; ++i)
            {
                int reference = reader.ReadInt32();

                Materials.Add(fragments[reference - 1] as Material);
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x30: Material count: " + Materials.Count);

            string references = string.Empty;

            for (var i = 0; i < Materials.Count; i++)
            {
                if (i != 0)
                {
                    references += ", ";
                }

                references += (Materials[i].Index + 1);
            }

            logger.LogInfo("0x30: References: " + references);
        }

        /// <summary>
        /// Creates a mapping of slot names to the index in which those materials are referenced in the list
        /// </summary>
        public void CreateIndexSlotMapping(ILogger logger)
        {
            logger.LogInfo("Creating slot mapping for: " + Name);     
            IndexSlotMapping = new Dictionary<string, int>();
            Skins = new Dictionary<int, Dictionary<int, Material>>(); 
            GeneralTextures = new List<Material>();
            
            for (int i = 0; i < Materials.Count; i++)
            {
                Material material = Materials[i];

                logger.LogInfo("Processing: " + material.Name);
                
                if (material.Name.StartsWith("IVM"))
                {
                    logger.LogWarning("Skipping slot index mapping for invisible man material");
                    continue;
                }

                if (!IsSlotMaterial(material.Name))
                {
                    GeneralTextures.Add(material);
                    //continue;
                }
                
                string slotKey = GetSlotKey(material.Name);

                int skinId = GetSkinId(material.Name);
                
                if (string.IsNullOrEmpty(slotKey))
                {
                    logger.LogError("Could not create slot mapping for material: " + material.Name);
                    continue;
                }

                if (!Skins.ContainsKey(skinId))
                {
                    Skins[skinId] = new Dictionary<int, Material>();
                }
                
                if (skinId == 0)
                {
                    IndexSlotMapping[slotKey] = i;
                }

                material.SlotKey = slotKey;
                material.IsHandled = true;
                material.ExportName = GetExportName(material.Name);
                Skins[skinId][i] = material;

                logger.LogInfo("Adding slot mapping for skinId: " + skinId +  " with slotkey: " + slotKey + " with name: " + GetExportName(material.Name));
            }
        }

        private int GetSkinId(string materialName)
        {
            string character;
            string skinId;
            string partName;

            if (materialName.Length < 13)
            {
                return 0;
            }
            
            SeparateCharacterMaterialName(materialName, out character, out skinId, out partName);

            int skinIdInt = 0;

            if (!int.TryParse(skinId, out skinIdInt))
            {
                Console.WriteLine("Material cannot be assigned: " + materialName);
                return 0;
            }

            return skinIdInt;
        }

        /// <summary>
        /// Parses the info from the material name
        /// </summary>
        /// <param name="materialName">The name of the material</param>
        /// <param name="character">The character mesh it belongs to</param>
        /// <param name="skinId">The skin ID</param>
        /// <param name="partName">The name of the body part this material is applied to</param>
        private static void SeparateCharacterMaterialName(string materialName, out string character, out string skinId,
            out string partName)
        {
            character = materialName.Substring(0, 3);
            skinId = materialName.Substring(5, 2);
            partName = materialName.Substring(3, 2) + materialName.Substring(7, 2);
        }

        /// <summary>
        /// Adds a material to a model skin list
        /// </summary>
        /// <param name="material">The material to add</param>
        /// <param name="logger">For debugging</param>
        public void AddMaterialToSkins(Material material, ILogger logger)
        {
            string character;
            string skinId;
            string partName;
            
            if (material.Name.Length < 13)
            {
                return;
            }

            SeparateCharacterMaterialName(material.Name, out character, out skinId, out partName);

            int skinIdInt = 0;

            if (!int.TryParse(skinId, out skinIdInt))
            {
                logger.LogError("Material cannot be assigned: " + material.Name);
                return;
            }

            string slotKey = partName.ToLower();

            // Find the position index
            if (!IndexSlotMapping.ContainsKey(slotKey))
            {
                logger.LogError("Cannot handle material: " + material.Name);
                return;
            }

            int slotIndex = IndexSlotMapping[slotKey];

            if (!Skins.ContainsKey(skinIdInt))
            {
                Skins[skinIdInt] = new Dictionary<int, Material>();
            }

            Skins[skinIdInt][slotIndex] = material;
            Materials.Add(material);
            material.SlotKey = slotKey.ToLower();
            material.ExportName = GetExportName(material.Name);
            material.IsHandled = true;
        }

        /// <summary>
        /// Returns the material that matches the supplied name but in a different slot
        /// For example, you can pass in the arm material and specify skin 3 and it will return the plate material
        /// </summary>
        /// <param name="name"></param>
        /// <param name="skinIndex"></param>
        /// <returns>The material, if it was found</returns>
        public Material GetMaterialSkinWithId(string name, int skinIndex)
        {
            if (!Skins.ContainsKey(skinIndex))
            {
                return null;
            }

            string slotKey = GetSlotKey(name);

            if (!IndexSlotMapping.ContainsKey(slotKey))
            {
                return null;
            }

            int slotIndex = IndexSlotMapping[slotKey];

            return !Skins[skinIndex].ContainsKey(slotIndex) ? null : Skins[skinIndex][slotIndex];
        }

        /// <summary>
        /// A helper method for returning a slot key based on the material name
        /// </summary>
        /// <param name="materialName">The material name</param>
        /// <returns>The slot key - or an empty string if it cannot be found</returns>
        private static string GetSlotKey(string materialName)
        {
            string character;
            string skinId;
            string partName;

            if (!IsSlotMaterial(materialName))
            {
                return materialName.Split('_')[0].ToLower();
            }

            SeparateCharacterMaterialName(materialName, out character, out skinId, out partName);

            return (partName).ToLower();
        }

        private static string GetExportName(string materialName)
        {
            string character;
            string skinId;
            string partName;

            if (!IsSlotMaterial(materialName))
            {
                return materialName.Split('_')[0].ToLower();
            }

            SeparateCharacterMaterialName(materialName, out character, out skinId, out partName);

            return (character + partName).ToLower();
        }

        private static bool IsSlotMaterial(string materialName)
        {
            string nameWithoutEnding = materialName.Split('_')[0];
            
            // Check first to see if this ends with 4 numbers (
            if (!Regex.Match(nameWithoutEnding, @"\d{4}$").Success)
            {
                return false;
            }
            
            // Ensure that this starts with 5 characters
            char[] array = nameWithoutEnding.ToCharArray();

            for (int i = 0; i < 5; ++i)
            {
                if (!char.IsLetter(array[i]))
                {
                    return false;
                }
            }

            return true;
        }
                
        public static string GetMtlExportName(string materialName)
        {
            string character;
            string skinId;
            string partName;

            if (!IsSlotMaterial(materialName))
            {
                return materialName.Split('_')[0];
            }

            SeparateCharacterMaterialName(materialName, out character, out skinId, out partName);

            return character + partName;
        }

        public string GetMaterialListExport()
        {
            var materialsExport = new StringBuilder();

            foreach (Material material in Materials)
            {
                if (material.IsInvisible)
                {
                    continue;
                }

                List<string> bitmapNames = material.GetAllBitmapNames();

                if (bitmapNames == null || bitmapNames.Count == 0)
                {
                    continue;
                }

                string pngName = bitmapNames[0].Substring(0, bitmapNames[0].Length - 4);

                materialsExport.AppendLine(LanternStrings.ObjNewMaterialPrefix + " " + GetMaterialPrefix(material.ShaderType) + pngName);
                materialsExport.AppendLine("Ka 1.000 1.000 1.000");
                materialsExport.AppendLine("Kd 1.000 1.000 1.000");
                materialsExport.AppendLine("Ks 0.000 0.000 0.000");
                materialsExport.AppendLine("d 1.0 ");
                materialsExport.AppendLine("illum 2");
                materialsExport.AppendLine("map_Kd " + pngName + ".png");
            }

            return materialsExport.ToString();
        }

        /// <summary>
        /// Gathers material lists for each skin variant to be output to .MTL files
        /// </summary>
        /// <returns>The list of material file content strings</returns>
        public List<string> GetMaterialSkinExports()
        {            
            var materialExports = new List<string>();
            var baseSkinMaterials = new List<string>();
            
            for (int i = 0; i < Skins.Count; ++i)
            {                
                var materialsExport = new StringBuilder();                       
                var currentUsedSlots = new List<string>();
                
                foreach (KeyValuePair<int, Material> materialMapping in Skins[i])
                {
                    Material material = materialMapping.Value;
                    
                    if (material == null)
                    {
                        continue;
                    }
                    
                    if (material.IsInvisible)
                    {
                        continue;
                    }

                    List<string> bitmapNames = material.GetAllBitmapNames();

                    if (bitmapNames == null || bitmapNames.Count == 0)
                    {
                        continue;
                    }

                    string pngName = bitmapNames[0].Substring(0, bitmapNames[0].Length - 4);

                    string textureName = pngName + ".png";

                    if (Name.ToLower().Contains("baf") && material.Name.ToLower().Contains("bam"))
                    {
                            
                    }

                    var headerName = material.ExportName;

                    materialsExport.AppendLine(GetMaterialExportChunk(headerName, textureName, material.ShaderType));
                    
                    if (i == 0)
                    {
                        if (material.Name.ToLower().Contains("chain"))
                        {
                            
                        }
                        baseSkinMaterials.Add(material.SlotKey);
                    }
                    else
                    {
                        currentUsedSlots.Add(material.SlotKey);
                    }
                }

                // If this is a skin variant, we need to ensure that all base slots are included.
                // If the skin variant does not have a matching entry for the base slot, we add the base slot material.
                if (i != 0)
                {
                    foreach (var baseMaterials in baseSkinMaterials)
                    {      
                        if (currentUsedSlots.Contains(baseMaterials))
                        {
                            continue;
                        }
                        
                        int slotIndex = IndexSlotMapping[baseMaterials];

                        Material materialToAdd = Skins[0][slotIndex];
                            
                        string pngName = materialToAdd.GetAllBitmapNames()[0];
                        pngName = pngName.Substring(0, pngName.Length - 4);

                        string textureName = pngName + ".png";

                        var headerName = materialToAdd.ExportName;

                        if (headerName == "")
                        {
                            
                        }

                        materialsExport.AppendLine(GetMaterialExportChunk(headerName, textureName, materialToAdd.ShaderType));
                    }
                }

                foreach (var general in GeneralTextures)
                {
                    Material materialToAdd = general;
                            
                    string pngName = materialToAdd.GetAllBitmapNames()[0];
                    pngName = pngName.Substring(0, pngName.Length - 4);

                    string textureName = pngName + ".png";

                    var headerName = materialToAdd.ExportName;

                    materialsExport.AppendLine(GetMaterialExportChunk(headerName, textureName, materialToAdd.ShaderType));
                }
                
                
                materialExports.Add(materialsExport.ToString());
            }
            
            return materialExports;
        }

        // TODO: Use this
        private string GetMaterialExportChunk(string materialName, string textureName, ShaderType shaderType)
        {
            StringBuilder chunk = new StringBuilder();
            chunk.AppendLine(LanternStrings.ObjNewMaterialPrefix + GetMaterialPrefix(shaderType) + materialName);
            chunk.AppendLine("Ka 1.000 1.000 1.000");
            chunk.AppendLine("Kd 1.000 1.000 1.000");
            chunk.AppendLine("Ks 0.000 0.000 0.000");
            chunk.AppendLine("d 1.0 ");
            chunk.AppendLine("illum 2");
            chunk.AppendLine("map_Kd " + textureName);

            return chunk.ToString();
        }

        public static string GetMaterialPrefix(ShaderType shaderType)
        {
            switch (shaderType)
            {
                case ShaderType.Diffuse:
                    return "d_";
                case ShaderType.Transparent25:
                    return "t25_";
                case ShaderType.Transparent50:
                    return "t50_";
                case ShaderType.Transparent75:
                    return "t75_";
                case ShaderType.TransparentAdditive:
                    return "ta_";
                case ShaderType.TransparentAdditiveUnlit:
                    return "tau_";
                case ShaderType.TransparentMasked:
                    return "tm_";
                case ShaderType.DiffuseSkydome:
                    return "ds_";
                case ShaderType.TransparentSkydome:
                    return "ts_";
                case ShaderType.TransparentAdditiveUnlitSkydome:
                    return "taus_";
                default:
                    return "?";
            }
        }
    }
}
