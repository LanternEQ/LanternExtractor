namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// The possible shaders that the EQ client uses to render each surface
    /// </summary>
    public enum ShaderType
    {
        /// <summary>
        /// A simple diffuse
        /// </summary>
        Diffuse,

        /// <summary>
        /// The surface is rendered with an alpha value 
        /// </summary>
        Transparent,

        /// <summary>
        /// A simple diffuse shader with support for masking out a color
        /// </summary>
        MaskedDiffuse,

        /// <summary>
        /// The transparent surface which also supports masking
        /// </summary>
        MaskedTransparent,

        /// <summary>
        /// Alpha value is computed based on the brightness.
        /// This shader can also be referred to as MaskedTransparent
        /// Complete black = 100% transparency
        /// </summary>
        AlphaFromBrightness,

        /// <summary>
        /// The surface is invisible and is not rendered
        /// </summary>
        Invisible
    };
}