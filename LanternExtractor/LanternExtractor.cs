using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Sound;
using LanternExtractor.EQ.Wld;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor
{
    static class LanternExtractor
    {
        private static Settings _settings;
        private static ILogger _logger;

        /// <summary>
        /// Threaded extraction utilizes tasks
        /// </summary>
        private static bool _useThreading = false;

        private static void Main(string[] args)
        {
            _logger = new TextFileLogger("log.txt");
            _settings = new Settings("settings.txt", _logger);
            _settings.Initialize();
            _logger.SetVerbosity((LogVerbosity) _settings.LoggerVerbosity);

            string archiveName;

            DateTime start = DateTime.Now;

#if DEBUG
            archiveName = "arena";
#else
            if (args.Length != 1)
            {
                Console.WriteLine("Format: lantern.exe <filename/shortname/all>");
                return;
            }
            
            archiveName = args[0];
#endif

            List<string> eqFiles = EqFileHelper.GetValidEqFilePaths(_settings.EverQuestDirectory, archiveName);

            if (eqFiles.Count == 0)
            {
                Console.WriteLine("No valid EQ files found for: '" + archiveName + "' at path: " +
                                  _settings.EverQuestDirectory);
                return;
            }

            if (_useThreading)
            {
                List<Task> tasks = new List<Task>();

                foreach (var file in eqFiles)
                {
                    string fileName = file;
                    Task task = Task.Factory.StartNew(() =>
                    {
                        ExtractArchive(fileName);
                    });
                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());
            }
            else
            {
                foreach (var file in eqFiles)
                {
                    ExtractArchive(file);
                }
            }

            Console.WriteLine($"Extraction complete ({(DateTime.Now - start).TotalSeconds})s");
        }

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

            string archiveName = Path.GetFileNameWithoutExtension(path);

            if (string.IsNullOrEmpty(archiveName))
            {
                return;
            }

            string shortName = archiveName.Split('_')[0];

            // For non WLD files, we can just extract their contents
            // Used for pure texture archives (e.g. bmpwad.s3d) and sound archives (e.g. snd1.pfs)
            if (!s3dArchive.IsWldArchive)
            {
                s3dArchive.WriteAllFiles(null, shortName + "/");
                return;
            }

            string wldFileName = archiveName + LanternStrings.WldFormatExtension;

            PfsFile wldFileInArchive = s3dArchive.GetFile(wldFileName);

            if (wldFileInArchive == null)
            {
                _logger.LogError($"Unable to extract WLD file {wldFileName} from archive: {path}");
                return;
            }

            if (EqFileHelper.IsModelsArchive(archiveName))
            {
                var wldFile = new WldFileModels(wldFileInArchive, shortName, WldType.Models, _logger, _settings);
                wldFile.Initialize();
                s3dArchive.WriteAllFiles(wldFile.GetMaskedTextures(), shortName + "/Models/Textures/", true);
            }
            else if (EqFileHelper.IsSkyArchive(archiveName))
            {
                var wldFile = new WldFileSky(wldFileInArchive, shortName, WldType.Sky, _logger, _settings);
                wldFile.Initialize();
                s3dArchive.WriteAllFiles(wldFile.GetMaskedTextures(), "sky/Textures/", true);
            }
            else if (EqFileHelper.IsCharactersArchive(archiveName))
            {
                WldFileCharacters wldFileToInject = null;

                if (archiveName.StartsWith("global3_chr"))
                {
                    var s3dArchive2 = new PfsArchive(path.Replace("global3_chr", "global_chr"), _logger);

                    if (!s3dArchive2.Initialize())
                    {
                        _logger.LogError("Failed to initialize PFS archive at path: " + path);
                        return;
                    }

                    PfsFile wldFileInArchive2 = s3dArchive2.GetFile("global_chr.wld");

                    wldFileToInject = new WldFileCharacters(wldFileInArchive2, "global_chr", WldType.Characters,
                        _logger, _settings);
                    wldFileToInject.Initialize(false);
                }

                var wldFile = new WldFileCharacters(wldFileInArchive, shortName, WldType.Characters,
                    _logger, _settings, wldFileToInject);
                wldFile.Initialize();

                string exportPath = _settings.ExportAllCharacterToSingleFolder
                    ? "all/Characters/Textures/"
                    : shortName + "/Characters/Textures/";

                s3dArchive.FilenameChanges = wldFile.FilenameChanges;
                s3dArchive.WriteAllFiles(wldFile.GetMaskedTextures(), exportPath, true);
            }
            else if (EqFileHelper.IsObjectsArchive(archiveName))
            {
                var wldFile = new WldFileObjects(wldFileInArchive, shortName, WldType.Objects, _logger, _settings);
                wldFile.Initialize();
                s3dArchive.WriteAllFiles(wldFile.GetMaskedTextures(), shortName + "/Objects/Textures/", true);
            }
            else
            {
                var wldFile = new WldFileZone(wldFileInArchive, shortName, WldType.Zone, _logger, _settings);
                wldFile.Initialize();
                s3dArchive.WriteAllFiles(wldFile.GetMaskedTextures(), shortName + "/Zone/Textures/", true);

                PfsFile lightsFileInArchive = s3dArchive.GetFile("lights" + LanternStrings.WldFormatExtension);

                if (lightsFileInArchive != null)
                {
                    var lightsWldFile = new WldFileLights(lightsFileInArchive, shortName, WldType.Lights, _logger, _settings);
                    lightsWldFile.Initialize();
                }

                PfsFile zoneObjectsFileInArchive = s3dArchive.GetFile("objects" + LanternStrings.WldFormatExtension);

                if (zoneObjectsFileInArchive != null)
                {
                    WldFileZoneObjects zoneObjectsWldFile = new WldFileZoneObjects(zoneObjectsFileInArchive, shortName,
                        WldType.ZoneObjects, _logger, _settings);
                    zoneObjectsWldFile.Initialize();
                }

                ExtractSoundData(shortName);
            }
        }

        private static void ExtractSoundData(string shortName)
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