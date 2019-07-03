using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public static class TextureConstants
    {
        public static int Boundaries = 0;                                 // 0000 0000 0000 0000 0000 0000 0000 0000 - CONFIRMED
        public static int Diffuse = -2147483647;                          // 1000 0000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int Transparent25 = -2147483639;                    // 1001 0000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int Transparent50 = -2147483643;                    // 1010 0000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int Transparent75 = -2147483638;                    // 0101 0000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int TransparentAdditive = -2147483625;              // 1110 1000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int TransparentAdditiveUnlit = -2147483637;         // 1101 0000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int TransparentMasked = -2147483629;                // 1100 1000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int TransparentMaskedPassable = -2147483641;        // 1110 0000 0000 0000 0000 0000 0000 0001 - CONFIRMED
        public static int DiffuseSkydome = -2147483635;                   // 1011 0000 0000 0000 0000 0000 0000 0001
        public static int TransparentSkydome = -2147483633;               // 1111 0000 0000 0000 0000 0000 0000 0001
        public static int TransparentAdditiveUnlitSkybox = -2147483632;   // 0000 1000 0000 0000 0000 0000 0000 0001
        public static int DiffuseUnknown1 = 1363;                         // 1100 1010 1010 0000 0000 0000 0000 0000
        public static int DiffuseUnknown2 = -2147483628;                  // 0010 1000 0000 0000 0000 0000 0000 0001
        public static int DiffuseUnknown3 = -2147483646;
        public static int DiffuseUnknown4 = -2147483627;
        public static int DiffuseUnknown5 = -2147483630;
        public static int DiffuseUnknown6 = -2147483599;
        public static int DiffuseUnknown7 = -2147483623;
        public static int InvisibleUnknown = 83;                          // 1100 1010 0000 0000 0000 0000 0000 0000 - CONFIRMED
        public static int InvisibleUnknown2 = 75;                         // 1101 0010 0000 0000 0000 0000 0000 0000 - CONFIRMED
        public static int InvisibleUnknown3 = -2147483645;
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

        public int Parameters;

        public string BitDump;

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

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
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
                TextureInfoReference = fragments[reference6 - 1] as TextureInfoReference;
            }

            bool[] bitDump = new bool[32];
            
            // The bits here determine what kind of shader this uses to render the material
            var bitAnalyzer = new BitAnalyzer((int)Parameters);
            bitDump[0] = bitAnalyzer.IsBitSet(0);
            bitDump[1] = bitAnalyzer.IsBitSet(1);
            bitDump[2] = bitAnalyzer.IsBitSet(2);
            bitDump[3] = bitAnalyzer.IsBitSet(3);
            bitDump[4] = bitAnalyzer.IsBitSet(4);
            bitDump[5] = bitAnalyzer.IsBitSet(5);
            bitDump[6] = bitAnalyzer.IsBitSet(6);
            bitDump[7] = bitAnalyzer.IsBitSet(7);
            bitDump[8] = bitAnalyzer.IsBitSet(8);
            bitDump[9] = bitAnalyzer.IsBitSet(9);
            bitDump[10] = bitAnalyzer.IsBitSet(10);
            bitDump[11] = bitAnalyzer.IsBitSet(11);
            bitDump[12] = bitAnalyzer.IsBitSet(12);
            bitDump[13] = bitAnalyzer.IsBitSet(13);
            bitDump[14] = bitAnalyzer.IsBitSet(14);
            bitDump[15] = bitAnalyzer.IsBitSet(15);
            bitDump[16] = bitAnalyzer.IsBitSet(16);
            bitDump[17] = bitAnalyzer.IsBitSet(17);
            bitDump[18] = bitAnalyzer.IsBitSet(18);
            bitDump[19] = bitAnalyzer.IsBitSet(19);
            bitDump[20] = bitAnalyzer.IsBitSet(20);
            bitDump[21] = bitAnalyzer.IsBitSet(21);
            bitDump[22] = bitAnalyzer.IsBitSet(22);
            bitDump[23] = bitAnalyzer.IsBitSet(23);
            bitDump[24] = bitAnalyzer.IsBitSet(24);
            bitDump[25] = bitAnalyzer.IsBitSet(25);
            bitDump[26] = bitAnalyzer.IsBitSet(26);
            bitDump[27] = bitAnalyzer.IsBitSet(27);
            bitDump[28] = bitAnalyzer.IsBitSet(28);
            bitDump[29] = bitAnalyzer.IsBitSet(29);
            bitDump[30] = bitAnalyzer.IsBitSet(30);
            bitDump[31] = bitAnalyzer.IsBitSet(31);
            
            
            for (int i = 0; i < 32; ++i)
            {
                if (i != 0 && i % 4 == 0)
                {
                    BitDump += " ";
                }
                
                BitDump += Convert.ToInt16(bitDump[i]);
            }
            
            if (Parameters == TextureConstants.Boundaries)
            {
                // Confirmed
                // Used for boundaries that are not rendered. TextInfoReference can be null or have reference.
                ShaderType = ShaderType.Invisible;
            }
            else if (Parameters == TextureConstants.Diffuse)
            {
                // Confirmed
                ShaderType = ShaderType.Diffuse;
            }
            else if (Parameters == TextureConstants.Transparent25)
            {
                // Confirmed
                // Blend strength 0.25
                ShaderType = ShaderType.Transparent25;
            }
            else if (Parameters == TextureConstants.Transparent50)
            {
                // Confirmed
                // Blend strength 0.5
                ShaderType = ShaderType.Transparent50;
            }
            else if (Parameters == TextureConstants.Transparent75)
            {
                // Confirmed
                // Blend strength 0.75
                ShaderType = ShaderType.Transparent75;
            }
            else if (Parameters == TextureConstants.TransparentAdditive)
            {
                // Confirmed
                // Need to confirm blend strength. Most likely 0.75
                ShaderType = ShaderType.TransparentAdditive;
            }
            else if (Parameters == TextureConstants.TransparentAdditiveUnlit)
            {
                // Confirmed
                // Blend strength 0.75
                ShaderType = ShaderType.TransparentAdditiveUnlit;
            }
            else if (Parameters == TextureConstants.TransparentMasked)
            {
                // Confirmed
                // TODO: Need to confirm the alpha cutoff
                ShaderType = ShaderType.TransparentMasked;
            }
            else if (Parameters == TextureConstants.TransparentMaskedPassable)
            {
                // Confirmed
                // TODO: Add special handling for values that are actually masked
                ShaderType = ShaderType.Diffuse;
            }
            else if (Parameters == TextureConstants.DiffuseSkydome) // Confirmed - only in sky
            {
                // Confirmed
                // Used in opaque sky meshes. No depth write.
                // Only used for: NORMALSKY, DESERTSKY, COTTONSKY, REDCLOUD
                ShaderType = ShaderType.DiffuseSkydome;
            }
            else if (Parameters == TextureConstants.TransparentSkydome)
            {
                // Confirmed
                // Used in transparent sky meshes. No depth write.
                // Only used for: DESERTCLOUD, FLUFFYCLOUD
                ShaderType = ShaderType.TransparentSkydome;
            }
            else if (Parameters == TextureConstants.TransparentAdditiveUnlitSkybox)
            {
                // Confirmed
                // TransparentAdditiveUnlitNoDepth - Used in transparent sky objects with additive blending.
                // Only used for: MOON, SUN, SATURN, MOON32, CRESCENT
                ShaderType = ShaderType.TransparentAdditiveUnlitSkydome;                
            }  
            else if (Parameters == TextureConstants.DiffuseUnknown1)
            {
                // ~255 instances
                // stumptop (airplane + many others), akapanel + 6 identcal (akanon), clnhn0002 + 30+ other (chequip), etc.
                // Many of them are irregularly shaped and the entire texture is not used, just a portion     
                ShaderType = ShaderType.Diffuse;
            }        
            else if (Parameters == TextureConstants.DiffuseUnknown2)
            {
                // 6000+ instances
                // Used for many character models (skinned meshes?)
                ShaderType = ShaderType.Diffuse;
            }
            else if (Parameters == TextureConstants.DiffuseUnknown3)
            {
                // ~31 instances
                // akabside (akanon), templplain + 3 (kerraridge), sidewalk + 24 (unrest)
                // Unknown why there are so many in unrest
                ShaderType = ShaderType.Diffuse;
            }
            else if (Parameters == TextureConstants.DiffuseUnknown4)
            {
                // 2 instances
                // bathn0001 and bathn0002 (chequip)
                // Assumed to be diffuse
                ShaderType = ShaderType.Diffuse;
            }
            else if(Parameters == TextureConstants.DiffuseUnknown5)
            {
                // 3 instances
                // scahe0004 (chequip), acigar (gequip), cigartip (gequip)
                // TODO: Investigate further in client
                ShaderType = ShaderType.Diffuse;
            }
            else if (Parameters == TextureConstants.DiffuseUnknown6)
            {
                // 1 instance
                // pipe-mouth (gequip)
                // TODO: Investigate further in client
                ShaderType = ShaderType.Diffuse;            
            }
            else if (Parameters == TextureConstants.DiffuseUnknown7)
            {
                // 9 instances
                // it154trans + 2 duplicates (gequip), coldaindecal (gequip2), iit555dwm03 + 4 similar (gequip2)
                // TODO: Investigate further in client
                ShaderType = ShaderType.Diffuse;
            }
            else if (Parameters == TextureConstants.InvisibleUnknown)
            {
                // Confirmed
                // All texture references null
                // 7 instances - no correlation what they are or where they are found
                ShaderType = ShaderType.Invisible;
            }
            else if (Parameters == TextureConstants.InvisibleUnknown2)
            {
                // Confirmed
                // All texture references null
                // 2 instances in hateplane: statue20 and well0
                ShaderType = ShaderType.Invisible;
            }
            else if (Parameters == TextureConstants.InvisibleUnknown3)
            {
                // Confirmed
                // All texture references null
                // 2 instances in soldungb: M0009_MDF and M0010_MDF
                ShaderType = ShaderType.Invisible;
            }
            else
            {
                ShaderType = TextureInfoReference == null ? ShaderType.Invisible : ShaderType.Diffuse;
            }   
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x30: Display type: " + ShaderType);
            logger.LogInfo("0x30: Parameters: " + Parameters);
            logger.LogInfo("0x30 Bit dump: " + BitDump);
            logger.LogInfo("0x30: Invisible: " + IsInvisible);
            logger.LogInfo("0x30: UnknownFloat1: " + UnknownFloat1);
            logger.LogInfo("0x30: UnknownFloat2: " + UnknownFloat2);

            if (ShaderType != ShaderType.Invisible && TextureInfoReference != null)
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