using System;
using System.Collections.Generic;
using System.IO;

namespace LanternExtractor.EQ.Eqg.DataTypes
{
    public enum EqgMaterialPropertyType
    {
        Float = 0,
        Int = 1,
        Texture = 2,
        Color = 3
    }

    public class EqgMaterialProperty
    {
        public string Name { get; set; }
        public EqgMaterialPropertyType Type { get; set; }
        public float FloatValue { get; set; }
        public uint IntValue { get; set; }
        public string StringValue { get; set; }
    }

    /// <summary>
    /// A material used by EQG meshes. Properties reference textures and shader parameters.
    /// </summary>
    public class EqgMaterial
    {
        public const string DiffuseTextureProperty = "e_TextureDiffuse0";
        public const string NormalTextureProperty = "e_TextureNormal0";

        public int Index { get; set; }
        public string Name { get; set; }
        public string Shader { get; set; }
        public List<EqgMaterialProperty> Properties { get; } = new List<EqgMaterialProperty>();

        public string DiffuseTexture => GetTexture(DiffuseTextureProperty);
        public string NormalTexture => GetTexture(NormalTextureProperty);

        public bool IsAlphaBlended => StartsWith("Alpha") || StartsWith("Add");
        public bool IsAlphaMasked => StartsWith("Chroma");

        /// <summary>
        /// The lowercase diffuse texture name without extension, or null if the material has no texture
        /// </summary>
        public string DiffuseTextureBaseName =>
            string.IsNullOrEmpty(DiffuseTexture) ? null : Path.GetFileNameWithoutExtension(DiffuseTexture).ToLower();

        /// <summary>
        /// The name used for this material in exported files. Materials are named after their diffuse
        /// texture, with a suffix for transparent shaders, so identical materials are shared across models.
        /// </summary>
        public string ExportName
        {
            get
            {
                var textureName = DiffuseTextureBaseName;
                if (textureName == null)
                {
                    return null;
                }

                return IsAlphaBlended ? $"{textureName}_blend" : IsAlphaMasked ? $"{textureName}_mask" : textureName;
            }
        }

        private string GetTexture(string propertyName)
        {
            foreach (var property in Properties)
            {
                if (property.Type == EqgMaterialPropertyType.Texture &&
                    string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.StringValue;
                }
            }

            return null;
        }

        private bool StartsWith(string prefix)
        {
            return Shader != null && Shader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
