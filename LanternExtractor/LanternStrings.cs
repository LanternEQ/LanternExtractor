namespace LanternExtractor
{
    /// <summary>
    /// A collection of strings that can be referenced from anywhere
    /// </summary>
    public static class LanternStrings
    {
        public const string ExportHeaderTitle = "# Lantern Extractor 0.2 - ";
        public const string ExportHeaderFormat = "# Format: ";
        public const string ExportZoneFolder = "Zone/";
        public const string ExportObjectsFolder = "Objects/";
        public const string ExportCharactersFolder = "Characters/";
        public const string ExportModelsFolder = "Models";

        public const string FormatMtlExtension = ".mtl";

        public const string ObjMaterialHeader = "mtllib ";
        public const string ObjUseMtlPrefix = "usemtl ";
        public const string ObjNewMaterialPrefix = "newmtl";
        public const string ObjFormatExtension = ".obj";

        public const string WldFormatExtension = ".wld";
        public const string S3dFormatExtension = ".s3d";
        public const string PfsFormatExtension = ".s3d";
        public const string SoundFormatExtension = ".eff";

        public const string TextExportSeparator = ",";
    }
}