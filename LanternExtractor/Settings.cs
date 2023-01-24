using System;
using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;


namespace LanternExtractor
{
    public enum ModelExportFormat
    {
        Intermediate = 0,
        Obj = 1,
        GlTF = 2
    }

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

        /// <summary>
        /// The OS path to the EverQuest directory
        /// </summary>
        public string EverQuestDirectory { get; private set; }

        /// <summary>
        /// Extract data from the WLD file
        /// If false, we just extract the S3D contents
        /// </summary>
        public bool RawS3dExtract { get; private set; }

        /// <summary>
        /// Adds group separation in the zone mesh export
        /// </summary>
        public bool ExportZoneMeshGroups { get; private set; }

        /// <summary>
        /// Exports hidden geometry like zone boundaries
        /// </summary>
        public bool ExportHiddenGeometry { get; private set; }

        /// <summary>
        /// Sets the desired model export format
        /// </summary>
        public ModelExportFormat ModelExportFormat { get; private set; }

        /// <summary>
        /// Sets the desired model export format
        /// </summary>
        public bool ExportCharactersToSingleFolder { get; private set; }

        /// <summary>
        /// Sets the desired model export format
        /// </summary>
        public bool ExportEquipmentToSingleFolder { get; private set; }

        /// <summary>
        /// Export all sound files to a single folder
        /// </summary>
        public bool ExportSoundsToSingleFolder { get; private set; }

        /// <summary>
        /// Exports all OBJ frames for all animations
        /// </summary>
        public bool ExportAllAnimationFrames { get; private set; }

        /// <summary>
        /// Exports all OBJ frames for all animations
        /// </summary>
        public bool ExportZoneWithObjects { get; private set; }

        /// <summary>
        /// Export vertex colors with glTF model. Default behavior of glTF renderers
        /// is to mix the vertex color with the base color, which will not look right.
        /// Only turn this on if you intend to do some post-processing that
        /// requires vertex colors being present.
        /// </summary>
        public bool ExportGltfVertexColors { get; private set; }

        /// <summary>
        /// Exports glTF models in .GLB file format. GLB packages the .glTF json, the
        /// associated .bin, and all of the model's texture images into one file. This will
        /// take up more space since textures can't be shared, however, it will make models
        /// more portable.
        /// </summary>
        public bool ExportGltfInGlbFormat { get; private set; }

        /// <summary>
        /// Additional files that should be exported when extracting with `all` or `clientdata`
        /// </summary>
        public string ClientDataFiles { get; private set; }

        /// <summary>
        /// The verbosity of the logger
        /// </summary>
        public int LoggerVerbosity { get; private set; }

        /// <summary>
        /// Constructor which caches the settings file path and the logger
        /// Also sets defaults for the settings in the case the file isn't found
        /// </summary>
        /// <param name="settingsFilePath">The OS path to the settings file</param>
        /// <param name="logger">A reference to the logger for debug info</param>
        public Settings(string settingsFilePath, ILogger logger)
        {
            _settingsFilePath = settingsFilePath;
            _logger = logger;

            EverQuestDirectory = "C:/EverQuest/";
            RawS3dExtract = false;
            ExportZoneMeshGroups = false;
            ExportHiddenGeometry = false;
            LoggerVerbosity = 0;
        }


        public void Initialize()
        {
            string settingsText;

            try
            {
                settingsText = File.ReadAllText(_settingsFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError("Error loading settings file: " + e.Message);
                return;
            }

            var parsedSettings = TextParser.ParseTextToDictionary(settingsText, '=', '#');

            if (parsedSettings == null)
            {
                return;
            }

            if (parsedSettings.ContainsKey("EverQuestDirectory"))
            {
                EverQuestDirectory = parsedSettings["EverQuestDirectory"];

                // Ensure the path ends with a /
                EverQuestDirectory = Path.GetFullPath(EverQuestDirectory + "/");
            }

            if (parsedSettings.ContainsKey("RawS3DExtract"))
            {
                RawS3dExtract = Convert.ToBoolean(parsedSettings["RawS3DExtract"]);
            }

            if (parsedSettings.ContainsKey("ExportZoneMeshGroups"))
            {
                ExportZoneMeshGroups = Convert.ToBoolean(parsedSettings["ExportZoneMeshGroups"]);
            }

            if (parsedSettings.ContainsKey("ExportHiddenGeometry"))
            {
                ExportHiddenGeometry = Convert.ToBoolean(parsedSettings["ExportHiddenGeometry"]);
            }

            if (parsedSettings.ContainsKey("ExportZoneWithObjects"))
            {
                ExportZoneWithObjects = Convert.ToBoolean(parsedSettings["ExportZoneWithObjects"]);
            }

            if (parsedSettings.ContainsKey("ModelExportFormat"))
            {
                var exportFormatSetting = (ModelExportFormat)Convert.ToInt32(parsedSettings["ModelExportFormat"]);
                ModelExportFormat = exportFormatSetting;
            }

            if (parsedSettings.ContainsKey("ExportCharacterToSingleFolder"))
            {
                ExportCharactersToSingleFolder = Convert.ToBoolean(parsedSettings["ExportCharacterToSingleFolder"]);
            }

            if (parsedSettings.ContainsKey("ExportEquipmentToSingleFolder"))
            {
                ExportEquipmentToSingleFolder = Convert.ToBoolean(parsedSettings["ExportEquipmentToSingleFolder"]);
            }

            if (parsedSettings.ContainsKey("ExportSoundsToSingleFolder"))
            {
                ExportSoundsToSingleFolder = Convert.ToBoolean(parsedSettings["ExportSoundsToSingleFolder"]);
            }

            if (parsedSettings.ContainsKey("ExportAllAnimationFrames"))
            {
                ExportAllAnimationFrames = Convert.ToBoolean(parsedSettings["ExportAllAnimationFrames"]);
            }

            if (parsedSettings.ContainsKey("ExportGltfVertexColors"))
            {
                ExportGltfVertexColors = Convert.ToBoolean(parsedSettings["ExportGltfVertexColors"]);
            }

            if (parsedSettings.ContainsKey("ExportGltfInGlbFormat"))
            {
                ExportGltfInGlbFormat = Convert.ToBoolean(parsedSettings["ExportGltfInGlbFormat"]);
            }

            if (parsedSettings.ContainsKey("ExportClientDataFiles"))
            {
                ClientDataFiles = parsedSettings["ExportClientDataFiles"];
            }

            if (parsedSettings.ContainsKey("LoggerVerbosity"))
            {
                LoggerVerbosity = Convert.ToInt32(parsedSettings["LoggerVerbosity"]);
            }
        }
    }
}
