namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// The possible shaders that the EQ client uses to render each surface
    /// </summary>
    public enum ShaderType
    {
        Diffuse = 0,
        Transparent25,
        Transparent50,
        Transparent75,       
        TransparentAdditive,
        TransparentAdditiveUnlit,
        TransparentMasked,
        DiffuseSkydome,
        TransparentSkydome, 
        TransparentAdditiveUnlitSkydome,
        Invisible
    };
}