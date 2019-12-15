using System;
using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor
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
                settingsText = System.IO.File.ReadAllText(_settingsFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError("Error loading settings file: " + e.Message);
                return;
            }

            Dictionary<string, string> parsedSettings = TextParser.ParseTextToDictionary(settingsText, '=', '#');

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

            if (parsedSettings.ContainsKey("LoggerVerbosity"))
            {
                LoggerVerbosity = Convert.ToInt32(parsedSettings["LoggerVerbosity"]);
            }
        }
    }
}