using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Sound;
using LanternExtractor.EQ.Wld;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor
{
    /// <summary>
    /// The main class in the application
    /// </summary>
    static class LanternExtractor
    {
        /// <summary>
        /// Settings - loaded before extraction
        /// </summary>
        private static Settings _settings;

        /// <summary>
        /// The logger - created before extraction and passed into files for logging
        /// </summary>
        private static ILogger _logger;
        
        /// <summary>
        /// Entry point for the application 
        /// </summary>
        /// <param name="args">Run arguments</param>
        static void Main(string[] args)
        {            
            _logger = new TextFileLogger("log.txt");
            _settings = new Settings("settings.txt", _logger);
            _settings.Initialize();
            _logger.SetVerbosity((LogVerbosity)_settings.LoggerVerbosity);

            string archiveName;
            
#if DEBUG
            archiveName = "chequip";          
#else
            if (args.Length != 1)
            {
                Console.WriteLine("Format: lantern.exe <filename/shortname/all>");
                return;
            }
            
            shortName = args[0];
#endif

            List<string> eqFiles = GetValidEqFilePaths(archiveName);

            if (eqFiles.Count == 0)
            {
                Console.WriteLine("No valid EQ files found for: '" + archiveName + "' at path: " + _settings.EverQuestDirectory);
                return;
            }      
                        
            foreach (var file in eqFiles)
            {
                ExtractArchive(file);
            }
            
            Console.WriteLine("Extraction complete");
        }

        /// <summary>
        /// Returns a list of valid file paths to extract based on the archive name input
        /// </summary>
        /// <param name="archiveName">The name of the archive to extract</param>
        /// <returns>Valid paths to extract</returns>
        private static List<string> GetValidEqFilePaths(string archiveName)
        {
            archiveName = archiveName.ToLower();
            
            var validFiles = new List<string>();

            if (archiveName == "all")
            {
                validFiles = Directory.GetFiles(_settings.EverQuestDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".s3d") || s.EndsWith(".pfs")).ToList();
            }
            else if (archiveName.EndsWith(".s3d") || archiveName.EndsWith(".pfs"))
            {
                string archivePath = _settings.EverQuestDirectory + archiveName;

                if (File.Exists(archivePath))
                {
                    validFiles.Add(archivePath);
                }
            }
            else
            {
                // If it's the shortname of a PFS file, we assume it's standalone - used for sound files
                string archivePath = _settings.EverQuestDirectory + archiveName + ".pfs";
                
                if (File.Exists(archivePath))
                {
                    validFiles.Add(archivePath);
                    
                    return validFiles;
                }
                
                // Try and find all associated files with the shortname - can theoretically be a non zone file
                string mainArchivePath = _settings.EverQuestDirectory + archiveName + ".s3d";
                if (File.Exists(mainArchivePath))
                {
                    validFiles.Add(mainArchivePath);
                }
                
                string objectsArchivePath = _settings.EverQuestDirectory + archiveName + "_obj.s3d";
                if (File.Exists(objectsArchivePath))
                {
                    validFiles.Add(objectsArchivePath);
                }
                
                string charactersArchivePath = _settings.EverQuestDirectory + archiveName + "_chr.s3d";
                if (File.Exists(charactersArchivePath))
                {
                    validFiles.Add(charactersArchivePath);
                }
            }

            return validFiles;
        }

        /// <summary>
        /// Initializes and extracts files from the specified archive
        /// </summary>
        /// <param name="path">The OS path to the file</param>
        private static void ExtractArchive(string path)
        {
            var s3dArchive = new PfsArchive(path, _logger);

            if (!s3dArchive.Initialize())
            {
                _logger.LogError("Failed to initialize PFS archive at path: " + path);
                return;
            }

            if (_settings.RawS3dExtract)
            {
                s3dArchive.WriteAllFiles(true);
                return;
            }

            // For non WLD files, we can just extract their contents
            // Used for pure texture archives (e.g. bmpwad.s3d)
            if (!s3dArchive.IsWldArchive)
            {
                s3dArchive.WriteAllFiles(null);
                return;
            }

            string archiveName = Path.GetFileNameWithoutExtension(path);

            if (string.IsNullOrEmpty(archiveName))
            {
                return;
            }
            
            string shortName = archiveName.Split('_')[0];
            string wldFileName = archiveName + LanternStrings.WldFormatExtension;
            
            PfsFile wldFileInArchive = s3dArchive.GetFile(wldFileName);

            if (wldFileInArchive == null)
            {
                _logger.LogError($"Unable to extract WLD file {wldFileName} from archive: {path}");
                return;
            }

            if (archiveName.Contains("_chr") || archiveName.Contains("_amr") || archiveName.StartsWith("chequip"))
            {
                WldFileCharacters wldFile = new WldFileCharacters(wldFileInArchive, shortName, WldType.Characters, _logger, _settings);
                wldFile.Initialize();
                s3dArchive.WriteAllFiles(wldFile.GetMaterialTypes(), "Characters", true);

            }
            else if (archiveName.Contains("_obj"))
            {
                WldFileObjects wldFile = new WldFileObjects(wldFileInArchive, shortName, WldType.Objects, _logger, _settings);
                wldFile.Initialize();
                s3dArchive.WriteAllFiles(wldFile.GetMaterialTypes(), "Objects", true);
            }
            else
            {
                WldFileZone wldFile = new WldFileZone(wldFileInArchive, shortName, WldType.Zone, _logger, _settings);
                wldFile.Initialize();
                s3dArchive.WriteAllFiles(wldFile.GetMaterialTypes(), "Zone", true);
                
                PfsFile lightsFileInArchive = s3dArchive.GetFile("lights" + LanternStrings.WldFormatExtension);

                if (lightsFileInArchive != null)
                {
                    WldFileLights lightsWldFile = new WldFileLights(lightsFileInArchive, shortName, WldType.Lights, _logger, _settings);
                    lightsWldFile.Initialize();
                }
                
                PfsFile zoneObjectsFileInArchive = s3dArchive.GetFile("objects" + LanternStrings.WldFormatExtension);

                if (zoneObjectsFileInArchive != null)
                {
                    WldFileZoneObjects zoneObjectsWldFile = new WldFileZoneObjects(zoneObjectsFileInArchive, shortName, WldType.ZoneObjects, _logger, _settings);
                    zoneObjectsWldFile.Initialize();
                }
                ExtractSoundFile(shortName);
            }
        }
        
        /// <summary>
        /// Parses and extracts the sound and music files for the zone
        /// </summary>
        /// <param name="shortName">The zone shortname</param>
        private static void ExtractSoundFile(string shortName)
        {
            var sounds = new EffSndBnk(_settings.EverQuestDirectory + shortName + "_sndbnk" +
                                       LanternStrings.SoundFormatExtension);
            sounds.Initialize();
            var soundEntries =
                new EffSounds(
                    _settings.EverQuestDirectory + shortName + "_sounds" + LanternStrings.SoundFormatExtension, sounds);
            soundEntries.Initialize();
            soundEntries.ExportSoundData(shortName);
        }
    }
}