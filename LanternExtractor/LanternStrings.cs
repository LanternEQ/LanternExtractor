namespace LanternExtractor
{
    /// <summary>
    /// A collection of strings that can be referenced from anywhere
    /// </summary>
    public static class LanternStrings
    {
        public const string ExportHeaderTitle = "# Lantern Extractor - ";
        public const string ExportHeaderFormat = "# Format: ";
        public const string ExportZoneFolder = "Zone/";
        public const string ExportObjectsFolder = "Objects/";
        public const string ExportCharactersFolder = "Characters/";

        public const string FormatMtlExtension = ".mtl";

        public const string ObjMaterialHeader = "mtllib ";
        public const string ObjUseMtlPrefix = "usemtl ";
        public const string ObjVertexPrefix = "v ";
        public const string ObjUvPrefix = "vt ";
        public const string ObjIndexPrefix = "f ";
        public const string ObjNewMaterialPrefix = "newmtl";
        public const string ObjFormatExtension = ".obj";

        public const string WldFormatExtension = ".wld";
        public const string PfsFormatExtension = ".s3d";
        public const string SoundFormatExtension = ".eff";
    }
}