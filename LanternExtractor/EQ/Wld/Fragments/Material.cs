using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.Helpers;
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
        /// The BitmapInfoReference that this material uses
        /// </summary>
        public BitmapInfoReference BitmapInfoReference { get; private set; }

        /// <summary>
        /// The shader type that this material uses when rendering
        /// </summary>
        public ShaderType ShaderType { get; set; }

        /// <summary>
        /// If a material has not been handled, we still need to find the corresponding material list
        /// Used for alternate character skins
        /// </summary>
        public bool IsHandled { get; set; }
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];
            int flags = reader.ReadInt32();
            int parameters = reader.ReadInt32();

            // Unsure what this color is used for
            byte colorR = reader.ReadByte();
            byte colorG = reader.ReadByte();
            byte colorB = reader.ReadByte();
            byte colorA = reader.ReadByte();

            float unknownFloat1 = reader.ReadSingle();
            float unknownFloat2 = reader.ReadSingle();

            int reference6 = reader.ReadInt32();

            if (reference6 != 0)
            {
                BitmapInfoReference = fragments[reference6 - 1] as BitmapInfoReference;
            }

            // Thanks to PixelBound for figuring this out
            MaterialType materialType = (MaterialType) (parameters & ~0x80000000);

            switch (materialType)
            {
                case MaterialType.Boundary:
                    ShaderType = ShaderType.Boundary;
                    break;
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
            
            CheckForSpecialCaseMasked();
        }

        /// <summary>
        /// These materials use an incorrectly flagged shader and should be marked as masked
        /// </summary>
        private void CheckForSpecialCaseMasked()
        {
            switch (Name)
            {
                case "TREE20_MDF":
                case "TOP_MDF":
                case "FURPILE1_MDF":
                case "BEARRUG_MDF":
                    ShaderType = ShaderType.TransparentMasked;
                    break;
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("Material: Shader type: " + ShaderType);

            if (ShaderType != ShaderType.Invisible && BitmapInfoReference != null)
            {
                logger.LogInfo("Material: Reference: " + (BitmapInfoReference.Index + 1));
            }
        }

        /// <summary>
        /// Returns all bitmap names referenced by this material
        /// </summary>
        /// <param name="includeExtension">Should be .bmp extension be included?</param>
        /// <returns>List of bitmap names</returns>
        public List<string> GetAllBitmapNames(bool includeExtension = false)
        {
            var bitmapNames = new List<string>();

            if (BitmapInfoReference == null)
            {
                return bitmapNames;
            }

            foreach (BitmapName bitmapName in BitmapInfoReference.BitmapInfo.BitmapNames)
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

        /// <summary>
        /// Returns the first bitmap name this material uses
        /// </summary>
        /// <returns></returns>
        public string GetFirstBitmapNameWithoutExtension()
        {
            if (BitmapInfoReference?.BitmapInfo?.BitmapNames == null || BitmapInfoReference.BitmapInfo.BitmapNames.Count == 0)
            {
                return string.Empty;
            }

            return BitmapInfoReference.BitmapInfo.BitmapNames[0].GetFilenameWithoutExtension();
        }

        public string GetFirstBitmapExportFilename()
        {
            if (BitmapInfoReference?.BitmapInfo?.BitmapNames == null || BitmapInfoReference.BitmapInfo.BitmapNames.Count == 0)
            {
                return string.Empty;
            }

            return BitmapInfoReference.BitmapInfo.BitmapNames[0].GetExportFilename();
        }

        public string GetFullMaterialName()
        {
            return MaterialList.GetMaterialPrefix(ShaderType) +
                    FragmentNameCleaner.CleanName(this);        
        }

        public void SetBitmapName(int index, string newName)
        {
            if (BitmapInfoReference == null)
            {
                return;
            }

            BitmapInfoReference.BitmapInfo.BitmapNames[index].Filename = newName + ".bmp";
        }
    }
}