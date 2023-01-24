using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Helpers
{
    /// <summary>
    /// Fixes issues with EverQuest materials.
    /// Currently, only fixes incorrect shader assignment
    /// but can be extended in the future.
    /// </summary>
    public static class MaterialFixer
    {
        public static void Fix(WldFile wld)
        {
            FixShaderAssignment(wld, "TREE20_MDF", ShaderType.TransparentMasked);
            FixShaderAssignment(wld, "TOP_MDF", ShaderType.TransparentMasked);
            FixShaderAssignment(wld, "FURPILE1_MDF", ShaderType.TransparentMasked);
            FixShaderAssignment(wld, "BEARRUG_MDF", ShaderType.TransparentMasked);
            FixShaderAssignment(wld, "FIRE1_MDF", ShaderType.TransparentAdditiveUnlit);
            FixShaderAssignment(wld, "ICE1_MDF", ShaderType.Invisible);
            FixShaderAssignment(wld, "AIRCLOUD_MDF", ShaderType.TransparentSkydome);
            FixShaderAssignment(wld, "NORMALCLOUD_MDF", ShaderType.TransparentSkydome);
        }

        private static void FixShaderAssignment(WldFile wld, string materialName, ShaderType shader)
        {
            var material = wld.GetFragmentByName<Material>(materialName);

            if (material != null)
            {
                material.ShaderType = shader;
            }
        }
    }
}