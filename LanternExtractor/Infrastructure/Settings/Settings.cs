using System;
using System.IO;
using Serilog;
using Tomlyn;

namespace LanternExtractor.Infrastructure.Settings
{
    /// <summary>
    /// Simple class that parses settings for the extractor
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// The OS path to the settings file
        /// </summary>
        private readonly string _settingsFilePath;

        // Properties with default values
        public string EverQuestDirectory { get; private set; } = "C:/EQxcdf/";
        public bool UseMultithreading { get; private set; } = false;
        public bool RawS3DExtract { get; private set; } = false;
        public ModelExportFormat ModelExportFormat { get; private set; } = ModelExportFormat.Intermediate;
        public bool ExportZoneMeshGroups { get; private set; } = false;
        public bool ExportHiddenGeometry { get; private set; } = false;
        public bool ExportCharactersToSingleFolder { get; private set; } = false;
        public bool ExportEquipmentToSingleFolder { get; private set; } = false;
        public bool ExportSoundsToSingleFolder { get; private set; } = false;
        public bool ExportFrontendToSingleFolder { get; private set; } = false;
        public bool ExportAllAnimationFrames { get; private set; } = false;
        public bool ExportZoneWithObjects { get; private set; } = false;
        public bool ExportGltfVertexColors { get; private set; } = false;
        public bool ExportGltfInGlbFormat { get; private set; } = false;
        public string[] ClientDataToCopy { get; private set; } = Array.Empty<string>();
        public bool CopyMusic { get; private set; } = false;
        public bool CopyVideo { get; private set; } = false;
        public int LoggerVerbosity { get; private set; } = 0;

        /// <summary>
        /// Constructor which caches the settings file path and the logger
        /// </summary>
        /// <param name="settingsFilePath">The OS path to the settings file</param>
        public Settings(string settingsFilePath)
        {
            _settingsFilePath = settingsFilePath;
        }

        /// <summary>
        /// Initializes the settings by reading from the TOML file
        /// </summary>
        public void Initialize()
{
    try
    {
        string settingsText = File.ReadAllText(_settingsFilePath);
        var document = Toml.Parse(settingsText);

        // Pass custom TomlModelOptions to prevent PascalCase -> snake_case conversion
        var options = new TomlModelOptions
        {
            ConvertPropertyName = (propertyName) => propertyName
        };

        var settingsData = document.ToModel<SettingsData>(options);

        EverQuestDirectory = NormalizePath(settingsData.EverQuestDirectory ?? EverQuestDirectory);
        UseMultithreading = settingsData.UseMultithreading ?? UseMultithreading;
        RawS3DExtract = settingsData.RawS3DExtract ?? RawS3DExtract;
        ModelExportFormat = settingsData.ModelExportFormat.HasValue ? (ModelExportFormat)settingsData.ModelExportFormat.Value : ModelExportFormat;
        ExportZoneMeshGroups = settingsData.ExportZoneMeshGroups ?? ExportZoneMeshGroups;
        ExportHiddenGeometry = settingsData.ExportHiddenGeometry ?? ExportHiddenGeometry;
        ExportCharactersToSingleFolder = settingsData.ExportCharacterToSingleFolder ?? ExportCharactersToSingleFolder;
        ExportEquipmentToSingleFolder = settingsData.ExportEquipmentToSingleFolder ?? ExportEquipmentToSingleFolder;
        ExportSoundsToSingleFolder = settingsData.ExportSoundsToSingleFolder ?? ExportSoundsToSingleFolder;
        ExportFrontendToSingleFolder = settingsData.ExportFrontendToSingleFolder ?? ExportFrontendToSingleFolder;
        ExportAllAnimationFrames = settingsData.ExportAllAnimationFrames ?? ExportAllAnimationFrames;
        ExportZoneWithObjects = settingsData.ExportZoneWithObjects ?? ExportZoneWithObjects;
        ExportGltfVertexColors = settingsData.ExportGltfVertexColors ?? ExportGltfVertexColors;
        ExportGltfInGlbFormat = settingsData.ExportGltfInGlbFormat ?? ExportGltfInGlbFormat;
        ClientDataToCopy = settingsData.ClientDataToCopy ?? ClientDataToCopy;
        CopyMusic = settingsData.CopyMusic ?? CopyMusic;
        CopyVideo = settingsData.CopyVideo ?? CopyVideo;
        LoggerVerbosity = settingsData.LoggerVerbosity ?? LoggerVerbosity;
    }
    catch (Exception e)
    {
        Log.Error("Error loading settings file: " + e.Message);
    }
}

/// <summary>
/// Normalize the directory path to handle OS-specific path separators.
/// </summary>
/// <param name="path">The raw path from the settings file.</param>
/// <returns>A normalized path with correct directory separators.</returns>
private string NormalizePath(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        return path;

    // Normalize slashes
    path = path.Replace('\\', Path.DirectorySeparatorChar);
    path = Path.GetFullPath(path + Path.DirectorySeparatorChar);

    return path;
}


        private class SettingsData
        {
            public string EverQuestDirectory { get; set; }
            public bool? UseMultithreading { get; set; }
            public bool? RawS3DExtract { get; set; }
            public int? ModelExportFormat { get; set; }
            public bool? ExportZoneMeshGroups { get; set; }
            public bool? ExportHiddenGeometry { get; set; }
            public bool? ExportCharacterToSingleFolder { get; set; }
            public bool? ExportEquipmentToSingleFolder { get; set; }
            public bool? ExportSoundsToSingleFolder { get; set; }
            public bool? ExportFrontendToSingleFolder { get; set; }
            public bool? ExportAllAnimationFrames { get; set; }
            public bool? ExportZoneWithObjects { get; set; }
            public bool? ExportGltfVertexColors { get; set; }
            public bool? ExportGltfInGlbFormat { get; set; }
            public string[] ClientDataToCopy { get; set; }
            public bool? CopyMusic { get; set; }
            public bool? CopyVideo { get; set; }
            public int? LoggerVerbosity { get; set; }
        }
    }
}
