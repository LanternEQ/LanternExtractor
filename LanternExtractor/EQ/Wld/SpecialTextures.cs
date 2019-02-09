namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// A list of textures that are of a specific type
    /// The original EQ client only supported flagging of regions as specific types
    /// In most modern engines, we will not use BSP based geometry but instead raycasts to determine if we are under water or lava.
    /// Therefore, in order to make things easier, we can export meshes containing only specific textures.
    /// Here is a list of water and lava textures so we can export these textured polygons as their own mesh.
    /// It's not ideal, but it does work and I can't think of a better solution.
    /// </summary>
    public static class SpecialTextures
    {
        /// <summary>
        /// Determines if the texture is a water texture
        /// </summary>
        /// <param name="textureName">The name of the texture</param>
        /// <returns>Whether or not the texture is water</returns>
        public static bool IsWater(string textureName)
        {
            if (textureName.Equals("w1.bmp"))
                return true;
            if (textureName.EndsWith("wguk1.bmp"))
                return true;
            if (textureName.EndsWith("water.bmp"))
                return true;
            if (textureName.EndsWith("agua1.bmp"))
                return true;
            if (textureName.EndsWith("grnwtr1.bmp"))
                return true;
            if (textureName.EndsWith("slime1.bmp"))
                return true;
            if (textureName.EndsWith("water1.bmp"))
                return true;
            if (textureName.EndsWith("charpoop1.bmp"))
                return true;
            if (textureName.EndsWith("charuwater1.bmp"))
                return true;
            if (textureName.EndsWith("blood1.bmp")) // may be lava?
                return true;
            if (textureName.EndsWith("gwater1.bmp"))
                return true;
            if (textureName.EndsWith("river1.bmp"))
                return true;
            if (textureName.Equals("sw1.bmp"))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if the texture is a lava texture
        /// </summary>
        /// <param name="textureName">The name of the texture</param>
        /// <returns>Whether or not the texture is water</returns>
        public static bool IsLava(string textureName)
        {
            if (textureName.EndsWith("lava001.bmp"))
                return true;
            if (textureName.EndsWith("b1.bmp"))
                return true;

            return false;
        }
    }
}