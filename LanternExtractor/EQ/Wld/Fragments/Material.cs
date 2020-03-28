using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public enum MaterialType
    {
        // Used for boundaries that are not rendered. TextInfoReference can be null or have reference.
        Boundary = 0x0,

        // Standard diffuse shader
        Diffuse = 0x01,

        // Diffuse variant
        Diffuse2 = 0x02,

        // Transparent with 0.5 blend strength
        Transparent50 = 0x05,

        // Transparent with 0.25 blend strength
        Transparent25 = 0x09,

        // Transparent with 0.75 blend strength
        Transparent75 = 0x0A,

        // Non solid surfaces that shouldn't really be masked
        TransparentMaskedPassable = 0x07,
        TransparentAdditiveUnlit = 0x0B,
        TransparentMasked = 0x13,
        Diffuse3 = 0x14,
        Diffuse4 = 0x15,
        TransparentAdditive = 0x17,
        Diffuse5 = 0x19,
        InvisibleUnknown = 0x53,
        Diffuse6 = 0x553,
        CompleteUnknown = 0x1A, // TODO: Analyze this
        Diffuse7 = 0x12,
        Diffuse8 = 0x31,
        InvisibleUnknown2 = 0x4B,
        DiffuseSkydome = 0x0D, // Need to confirm
        TransparentSkydome = 0x0F, // Need to confirm
        TransparentAdditiveUnlitSkydome = 0x10,
        InvisibleUnknown3 = 0x03,
    }

    /// <summary>
    /// 0x30 - Material
    /// Contains information about the material and how it's rendered
    /// </summary>
    public class Material : WldFragment
    {
        /// <summary>
        /// The TextureInfoReference (0x05) that this material uses
        /// </summary>
        public BitmapInfoReference BitmapInfoReference { get; private set; }

        /// <summary>
        /// The shader type that this material uses when rendering
        /// </summary>
        public ShaderType ShaderType { get; private set; }

        public string SlotKey { get; set; }

        public string ExportName { get; set; }

        public bool IsHandled { get; set; }

        public bool IsGlobalMaterial { get; set; }

        public int Parameters;

        private int colorR;
        private int colorG;
        private int colorB;
        private int colorA;

        public float UnknownFloat1 { get; set; }
        public float UnknownFloat2 { get; set; }

        public enum CharacterMaterialType
        {
            NormalTexture,
            CharacterSkin,
            GlobalSkin,
        }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            // String reference
            Name = stringHash[-reader.ReadInt32()];

            // Flags?
            int flags = reader.ReadInt32();

            // Params
            Parameters = reader.ReadInt32();

            colorR = reader.ReadByte();
            colorG = reader.ReadByte();
            colorB = reader.ReadByte();
            colorA = reader.ReadByte();

            UnknownFloat1 = reader.ReadSingle();

            UnknownFloat2 = reader.ReadSingle();

            int reference6 = reader.ReadInt32();

            if (reference6 != 0)
            {
                BitmapInfoReference = fragments[reference6 - 1] as BitmapInfoReference;
            }

            MaterialType materialType = (MaterialType) (Parameters & ~0x80000000);

            switch (materialType)
            {
                case MaterialType.Boundary:
                case MaterialType.InvisibleUnknown:
                case MaterialType.InvisibleUnknown2:
                case MaterialType.InvisibleUnknown3:
                    ShaderType = ShaderType.Invisible;
                    break;
                case MaterialType.Diffuse:
                case MaterialType.Diffuse3:
                case MaterialType.Diffuse4:
                case MaterialType.Diffuse6:
                case MaterialType.Diffuse5:
                case MaterialType.Diffuse7:
                case MaterialType.Diffuse8:
                case MaterialType.Diffuse2:
                case MaterialType.CompleteUnknown: // TODO: Figure out where this is used
                case MaterialType.TransparentMaskedPassable: // TODO: Add special handling
                    ShaderType = ShaderType.Diffuse;
                    break;
                case MaterialType.Transparent25:
                    ShaderType = ShaderType.Transparent25;
                    break;
                case MaterialType.Transparent50:
                    ShaderType = ShaderType.Transparent50;
                    break;
                case MaterialType.Transparent75:
                    ShaderType = ShaderType.Transparent75;
                    break;
                case MaterialType.TransparentAdditive:
                    ShaderType = ShaderType.TransparentAdditive;
                    break;
                case MaterialType.TransparentAdditiveUnlit:
                    ShaderType = ShaderType.TransparentAdditiveUnlit;
                    break;
                case MaterialType.TransparentMasked:
                    ShaderType = ShaderType.TransparentMasked;
                    break;
                case MaterialType.DiffuseSkydome:
                    ShaderType = ShaderType.DiffuseSkydome;
                    break;
                case MaterialType.TransparentSkydome:
                    ShaderType = ShaderType.TransparentSkydome;
                    break;
                case MaterialType.TransparentAdditiveUnlitSkydome:
                    ShaderType = ShaderType.TransparentAdditiveUnlitSkydome;
                    break;
                default:
                    ShaderType = BitmapInfoReference == null ? ShaderType.Invisible : ShaderType.Diffuse;
                    break;
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x30: Display type: " + ShaderType);
            logger.LogInfo("0x30: Parameters: " + Parameters);
            logger.LogInfo("0x30: UnknownFloat1: " + UnknownFloat1);
            logger.LogInfo("0x30: UnknownFloat2: " + UnknownFloat2);

            if (ShaderType != ShaderType.Invisible && BitmapInfoReference != null)
            {
                logger.LogInfo("0x30: Reference: " + (BitmapInfoReference.Index + 1));
            }
        }

        public List<string> GetAllBitmapNames(bool includeExtension = false)
        {
            var bitmapNames = new List<string>();

            if (BitmapInfoReference == null)
            {
                return bitmapNames;
            }

            foreach (Bitmap bitmapName in BitmapInfoReference.BitmapInfo.BitmapNames)
            {
                string filename = bitmapName.Filename;

                if (!includeExtension)
                {
                    filename = filename.Substring(0, filename.Length - 4);
                }
                
                bitmapNames.Add(filename);
            }

            return bitmapNames;
        }

        public bool GetIsSlotReference()
        {
            var materialName = Name.Split('_')[0];

            return Regex.Match(materialName, @"\d{4}$").Success;
        }

        public CharacterMaterialType GetMaterialType()
        {
            string nameWithoutEnding = Name.Split('_')[0];

            // Check first to see if this ends with 4 numbers - if not, it's not a skin
            if (!Regex.Match(nameWithoutEnding, @"\d{4}$").Success)
            {
                return CharacterMaterialType.NormalTexture;
            }

            // Ensure that this starts with 5 characters
            char[] array = nameWithoutEnding.ToCharArray();

            for (int i = 0; i < 5; ++i)
            {
                if (!char.IsLetter(array[i]))
                {
                    return CharacterMaterialType.GlobalSkin;
                }
            }

            return CharacterMaterialType.CharacterSkin;
        }

        public string GetFirstBitmapNameWithoutExtension()
        {
            if (BitmapInfoReference == null || BitmapInfoReference.BitmapInfo == null ||
                BitmapInfoReference.BitmapInfo.BitmapNames == null ||
                BitmapInfoReference.BitmapInfo.BitmapNames.Count == 0)
            {
                return string.Empty;
            }

            return BitmapInfoReference.BitmapInfo.BitmapNames[0].GetFilenameWithoutExtension();
        }

        public string GetSpecificMaterialSkinWithoutExtension(int specificSkin)
        {
            string charName;
            int skinId;
            string partName;

            string fileName = GetFirstBitmapNameWithoutExtension() + "_mdf";


            if (!WldMaterialPalette.ExplodeName(fileName.ToUpper(), out charName, out skinId, out partName))
            {
                return string.Empty;
            }

            string skinIdText = skinId >= 10 ? skinId.ToString() : "0" + specificSkin;

            var final = charName + partName.Substring(0, 2) + skinIdText + partName.Substring(2, 2);

            return final;
        }

        public string GetMaterialSkinWithoutExtension()
        {
            string charName;
            int skinId;
            string partName;

            string fileName = GetFirstBitmapNameWithoutExtension() + "_mdf";

            if (!WldMaterialPalette.ExplodeName(fileName.ToUpper(), out charName, out skinId, out partName))
            {
                return string.Empty;
            }

            var final = charName + partName.Substring(0, 2) + "{ID}" + partName.Substring(2, 2);

            return final;
        }

        public string GetFirstBitmapExportFilename()
        {
            if (BitmapInfoReference == null || BitmapInfoReference.BitmapInfo == null ||
                BitmapInfoReference.BitmapInfo.BitmapNames == null ||
                BitmapInfoReference.BitmapInfo.BitmapNames.Count == 0)
            {
                return string.Empty;
            }

            return BitmapInfoReference.BitmapInfo.BitmapNames[0].GetExportFilename();
        }

        public void SetHandled(bool b)
        {
            IsHandled = b;
        }

        public object GetMaterialNameNew(int skinId2)
        {
            string charName;
            int skinId;
            string partName;

            string fileName = GetFirstBitmapNameWithoutExtension() + "_mdf";

            if (!WldMaterialPalette.ExplodeName(fileName.ToUpper(), out charName, out skinId, out partName))
            {
                return string.Empty;
            }

            var final = charName + partName.Substring(0, 2) + "{ID}" + partName.Substring(2, 2);

            return final;
        }
    }
}