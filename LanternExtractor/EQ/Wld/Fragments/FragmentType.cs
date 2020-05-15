namespace LanternExtractor.EQ.Wld.Fragments
{
    public enum FragmentType
    {
        Bitmap = 0x03,
        BitmapInfo = 0x04,
        BitmapInfoReference = 0x05,
        Material = 0x30,
        MaterialList = 0x31,
        ObjectInstance = 0x15,
        Light = 0x1B,
        LightReference = 0x1C,
        LightInstance = 0x28,
        BspTree = 0x21,
        BspRegion = 0x22,
        BspRegionType = 0x29,
        AmbientLight = 0x2A,
        GlobalAmbientLightColor = 0x35,
        Mesh = 0x36,
        MeshReference = 0x2D,
        MeshVertexAnimation = 0x37,
        ModelReference = 0x14,
        VertexColor = 0x32,
        VertexColorReference = 0x33,
        Camera = 0x08,
        CameraReference = 0x09,
        
        // TODO: Rename these
        SkeletonHierarchy = 0x10,
        HierSpriteFragment = 0x11,
        TrackDefFragment = 0x12,
        TrackFragment = 0x13,
        
        Fragment16 = 0x16,
        Fragment17 = 0x17,
        Fragment18 = 0x18,
        Fragment2F = 0x2F
    }
}