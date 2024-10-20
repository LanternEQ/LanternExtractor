using System;
using System.IO;
using LanternExtractor.Infrastructure.Logger;
using Tomlyn;

namespace LanternExtractor.Infrastructure.Settings
{
    /// <summary>
    /// Simple class that parses settings for the extractor
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// The logger reference for debug output
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The OS path to the settings file
        /// </summary>
        private readonly string _settingsFilePath;

        // Properties with default values
        public string EverQuestDirectory { get; private set; } = "C:/EverQuest/";
        public bool RawS3DExtract { get; private set; } = false;
        public ModelExportFormat ModelExportFormat { get; private set; } = ModelExportFormat.Intermediate;
        public bool ExportZoneMeshGroups { get; private set; } = false;
        public bool ExportHiddenGeometry { get; private set; } = false;
        public bool ExportCharactersToSingleFolder { get; private set; } = false;
        public bool ExportEquipmentToSingleFolder { get; private set; } = false;
        public bool ExportSoundsToSingleFolder { get; private set; } = false;
        public bool ExportAllAnimationFrames { get; private set; } = false;
        public bool ExportZoneWithObjects { get; private set; } = false;
        public bool ExportGltfVertexColors { get; private set; } = false;
        public bool ExportGltfInGlbFormat { get; private set; } = false;
        public string[] ClientDataToCopy { get; private set; } = Array.Empty<string>();
        public bool CopyMusic { get; private set; } = false;
        public int LoggerVerbosity { get; private set; } = 0;

        /// <summary>
        /// Constructor which caches the settings file path and the logger
        /// </summary>
        /// <param name="settingsFilePath">The OS path to the settings file</param>
        /// <param name="logger">A reference to the logger for debug info</param>
        public Settings(string settingsFilePath, ILogger logger)
        {
            _settingsFilePath = settingsFilePath;
            _logger = logger;
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

                EverQuestDirectory = settingsData.EverQuestDirectory ?? EverQuestDirectory;
                RawS3DExtract = settingsData.RawS3DExtract ?? RawS3DExtract;
                ModelExportFormat = settingsData.ModelExportFormat.HasValue ? (ModelExportFormat)settingsData.ModelExportFormat.Value : ModelExportFormat;
                ExportZoneMeshGroups = settingsData.ExportZoneMeshGroups ?? ExportZoneMeshGroups;
                ExportHiddenGeometry = settingsData.ExportHiddenGeometry ?? ExportHiddenGeometry;
                ExportCharactersToSingleFolder = settingsData.ExportCharacterToSingleFolder ?? ExportCharactersToSingleFolder;
                ExportEquipmentToSingleFolder = settingsData.ExportEquipmentToSingleFolder ?? ExportEquipmentToSingleFolder;
                ExportSoundsToSingleFolder = settingsData.ExportSoundsToSingleFolder ?? ExportSoundsToSingleFolder;
                ExportAllAnimationFrames = settingsData.ExportAllAnimationFrames ?? ExportAllAnimationFrames;
                ExportZoneWithObjects = settingsData.ExportZoneWithObjects ?? ExportZoneWithObjects;
                ExportGltfVertexColors = settingsData.ExportGltfVertexColors ?? ExportGltfVertexColors;
                ExportGltfInGlbFormat = settingsData.ExportGltfInGlbFormat ?? ExportGltfInGlbFormat;
                ClientDataToCopy = settingsData.ClientDataToCopy ?? ClientDataToCopy;
                CopyMusic = settingsData.CopyMusic ?? CopyMusic;
                LoggerVerbosity = settingsData.LoggerVerbosity ?? LoggerVerbosity;

                EverQuestDirectory = Path.GetFullPath(EverQuestDirectory + Path.DirectorySeparatorChar);
            }
            catch (Exception e)
            {
                _logger.LogError("Error loading settings file: " + e.Message);
            }
        }

        private class SettingsData
        {
            public string EverQuestDirectory { get; set; }
            public bool? RawS3DExtract { get; set; }
            public int? ModelExportFormat { get; set; }
            public bool? ExportZoneMeshGroups { get; set; }
            public bool? ExportHiddenGeometry { get; set; }
            public bool? ExportCharacterToSingleFolder { get; set; }
            public bool? ExportEquipmentToSingleFolder { get; set; }
            public bool? ExportSoundsToSingleFolder { get; set; }
            public bool? ExportAllAnimationFrames { get; set; }
            public bool? ExportZoneWithObjects { get; set; }
            public bool? ExportGltfVertexColors { get; set; }
            public bool? ExportGltfInGlbFormat { get; set; }
            public string[] ClientDataToCopy { get; set; }
            public bool? CopyMusic { get; set; }
            public int? LoggerVerbosity { get; set; }
        }
    }
}
