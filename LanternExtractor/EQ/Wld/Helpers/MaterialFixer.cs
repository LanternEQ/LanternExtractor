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
        public static void Fix(WldFile wldFile)
        {
            FixShaderAssignment(wldFile);
        }

        private static void FixShaderAssignment(WldFile wld)
        {
            var materialFragments = wld.GetFragmentsOfType<Material>();

            foreach (var mf in materialFragments)
            {
                switch (mf.Name)
                {
                    case "TREE20_MDF":
                    case "TOP_MDF":
                    case "FURPILE1_MDF":
                    case "BEARRUG_MDF":
                        mf.ShaderType = ShaderType.TransparentMasked;
                        break;
                    case "FIRE1_MDF":
                        mf.ShaderType = ShaderType.TransparentAdditiveUnlit;
                        break;
                    case "ICE1_MDF":
                        mf.ShaderType = ShaderType.Invisible;
                        break;
                }
            }
        }
    }
}