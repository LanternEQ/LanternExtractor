using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public static class TextureConstants
    {
        public static int Diffuse = -2147483647;
        public static int Transparent = -2147483643;
        public static int MaskedDiffuse = -2147483629;
        public static int DiffusePassable = -2147483641;
        public static int MaskedTransparentLit = -2147483625; // just a guess - lanternglass
        public static int MaskedTransparentUnlit = -2147483637; // aka blackmask
        public static int UnknownDiffuse = 1363; // no idea what this does
        public static int UnknownDiffuse2 = -2147483628; // used for objects - usually bones and body parts
    }
    
    /// <summary>
    /// 0x30 - Material
    /// Contains information about the material and how it's rendered
    /// </summary>
    class Material : WldFragment
    {
        /// <summary>
        /// Is this material invisible? Used for invisible walls and things that are not rendered. 
        /// </summary>
        public bool IsInvisible { get; private set; }

        /// <summary>
        /// The TextureInfoReference (0x05) that this material uses
        /// </summary>
        public TextureInfoReference TextureInfoReference { get; private set; }

        /// <summary>
        /// The shader type that this material uses when rendering
        /// </summary>
        public ShaderType ShaderType { get; private set; }

        public string SlotKey { get; set; }

        public string ExportName { get; set; }

        public bool IsHandled { get; set; }

        public bool IsGlobalMaterial { get; set; }

        public enum CharacterMaterialType
        {
            NormalTexture,
            CharacterSkin,
            GlobalSkin,
        }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            // String reference
            Name = stringHash[-reader.ReadInt32()];

            // Flags?
            int flags = reader.ReadInt32();

            // Params
            int parameters = reader.ReadInt32();

            int params2 = reader.ReadInt32();

            float params3a = reader.ReadSingle();

            float params3b = reader.ReadSingle();

            int reference6 = reader.ReadInt32();

            if (reference6 != 0)
            {
                TextureInfoReference = fragments[reference6 - 1] as TextureInfoReference;
            }
            else
            {
                // Some materials are missing texture references
                // This may correspond with the 'palette.bmp' texture that is often unused
                // We consider them invisible
                IsInvisible = true;
            }

            // The bits here determine what kind of shader this uses to render the material
            var bitAnalyzer = new BitAnalyzer((int)parameters);
            bool bit0 = bitAnalyzer.IsBitSet(0);
            bool bit1 = bitAnalyzer.IsBitSet(1);
            bool bit2 = bitAnalyzer.IsBitSet(2);
            bool bit3 = bitAnalyzer.IsBitSet(3);
            bool bit4 = bitAnalyzer.IsBitSet(4);
            bool bit5 = bitAnalyzer.IsBitSet(5);
            bool bit6 = bitAnalyzer.IsBitSet(6);
            bool bit7 = bitAnalyzer.IsBitSet(7);
            
            if (parameters == 0)
            {
                // Invisible texture (used for things like boundaries that are not rendered)
                // All bits are 0
                ShaderType = ShaderType.Invisible;
                IsInvisible = true;
            }
            else if (parameters == TextureConstants.Diffuse)
            {
                // Diffuse - Fully opaque, used by most materials
                ShaderType = ShaderType.Diffuse;
            }
            else if (parameters == TextureConstants.Transparent)
            {
                // Transparent - Materials that are partly transparent (e.g. water)
                ShaderType = ShaderType.Transparent;
            }
            else if (parameters == TextureConstants.MaskedDiffuse)
            {
                // Masked - Opaque materials that have a color (top left) when rendering (e.g. tree leaves, rope)
                ShaderType = ShaderType.MaskedDiffuse;
            }
            else if (parameters == TextureConstants.DiffusePassable)
            {
                ShaderType = ShaderType.Diffuse;
            }
            else if (parameters == TextureConstants.MaskedTransparentUnlit)
            {
                ShaderType = ShaderType.AlphaFromBrightness;
            }
            else if (parameters == TextureConstants.MaskedTransparentLit)
            {
                ShaderType = ShaderType.AlphaFromBrightness;
            }
            else if (parameters == TextureConstants.UnknownDiffuse)
            {
                ShaderType = ShaderType.Diffuse;
                logger.LogError("Dump: " + parameters.ToString("X"));
            }
            else if (parameters == TextureConstants.UnknownDiffuse2)
            {
                ShaderType = ShaderType.Diffuse;
                logger.LogError("Dump: " + parameters.ToString("X"));
            }
            else
            {
                // Unhandled - default to Diffuse
                ShaderType = ShaderType.Diffuse;
                logger.LogError("Unable to identify shader type for material: " + Name);
                logger.LogError("Flag bit dump: " + bit0 + " " + bit1 + " " + bit2 + " " + bit3 + " " + bit4 + " " +
                                  bit5 + " " + bit6 + " " + bit7);
                logger.LogError("Param dump: " + parameters);
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x30: Display type: " + ShaderType);
            logger.LogInfo("0x30: Invisible: " + IsInvisible);

            if (!IsInvisible)
            {
                logger.LogInfo("0x30: Reference: " + (TextureInfoReference.Index + 1));
            }
        }

        public List<string> GetAllBitmapNames()
        {
            var bitmapNames = new List<string>();

            if (TextureInfoReference == null)
            {
                return bitmapNames;
            }

            foreach (BitmapName bitmapName in TextureInfoReference.TextureInfo.BitmapNames)
            {
                bitmapNames.Add(bitmapName.Filename);
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
    }
}