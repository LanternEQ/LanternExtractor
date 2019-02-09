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
        /// Export the main zone file and geometry
        /// </summary>
        public bool ExtractZoneFile { get; private set; }

        /// <summary>
        /// Export the objects file containing object geometry
        /// </summary>
        public bool ExtractObjectsFile { get; private set; }

        /// <summary>
        /// Export the character models
        /// </summary>
        public bool ExtractCharactersFile { get; private set; }

        /// <summary>
        /// Export the sound and music data
        /// </summary>
        public bool ExtractSoundFile { get; private set; }

        /// <summary>
        /// Extract data from the WLD file
        /// If false, we just extract the S3D contents
        /// </summary>
        public bool ExtractWld { get; private set; }
        
        /// <summary>
        /// Adds group separation in the zone mesh export
        /// </summary>
        public bool ExportZoneMeshGroups { get; private set; }

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
            ExtractZoneFile = true;
            ExtractObjectsFile = true;
            ExtractCharactersFile = true;
            ExtractSoundFile = false;
            ExtractWld = true;
            ExportZoneMeshGroups = false;
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

            if (parsedSettings.ContainsKey("ExtractZoneFile"))
            {
                ExtractZoneFile = Convert.ToBoolean(parsedSettings["ExtractZoneFile"]);
            }

            if (parsedSettings.ContainsKey("ExtractObjectsFile"))
            {
                ExtractObjectsFile = Convert.ToBoolean(parsedSettings["ExtractObjectsFile"]);
            }

            if (parsedSettings.ContainsKey("ExtractCharactersFile"))
            {
                ExtractCharactersFile = Convert.ToBoolean(parsedSettings["ExtractCharactersFile"]);
            }

            if (parsedSettings.ContainsKey("ExtractSoundFile"))
            {
                ExtractSoundFile = Convert.ToBoolean(parsedSettings["ExtractSoundFile"]);
            }

            if (parsedSettings.ContainsKey("ExtractWld"))
            {
                ExtractWld = Convert.ToBoolean(parsedSettings["ExtractWld"]);
            }
            
            if (parsedSettings.ContainsKey("ExportZoneMeshGroups"))
            {
                ExportZoneMeshGroups = Convert.ToBoolean(parsedSettings["ExportZoneMeshGroups"]);
            }

            _logger.LogInfo("Settings file successfully loaded!");
        }
    }
}