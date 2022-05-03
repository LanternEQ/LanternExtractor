﻿using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Sound;
using LanternExtractor.EQ.Wld;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ
{
    public static class ArchiveExtractor
    {
        public static void Extract(string path, string rootFolder, ILogger logger, Settings settings)
        {
            string archiveName = Path.GetFileNameWithoutExtension(path);

            if (string.IsNullOrEmpty(archiveName))
            {
                return;
            }

            // If ExportAdditionalAnimations then global_chr should already be initialized
            // and we can skip that step
            if (archiveName == "global_chr" && settings.ExportAdditionalAnimations)
            {
                var globalChrWld = GlobalReference.CharacterWld;
                if (globalChrWld != null)
                {
                    var exportPath = rootFolder + (settings.ExportCharactersToSingleFolder &&
                                    settings.ModelExportFormat == ModelExportFormat.Intermediate
                        ? "characters/Textures/"
                        : ShortnameHelper.GetCorrectZoneShortname("global") + "/Characters/Textures/");
                        
                    InitializeWldAndWriteTextures(globalChrWld, rootFolder, exportPath, GlobalReference.CharacterWldPfsArchive, settings, logger);

                    return;
                }
            }

            string shortName = archiveName.Split('_')[0];
            var s3dArchive = new PfsArchive(path, logger);

            if (!s3dArchive.Initialize())
            {
                logger.LogError("LanternExtractor: Failed to initialize PFS archive at path: " + path);
                return;
            }

            if (settings.RawS3dExtract)
            {
                s3dArchive.WriteAllFiles(Path.Combine(rootFolder + shortName, archiveName));
                return;
            }

            // For non WLD files, we can just extract their contents
            // Used for pure texture archives (e.g. bmpwad.s3d) and sound archives (e.g. snd1.pfs)
            // The difference between this and the raw export is that it will convert images to .png
            if (!s3dArchive.IsWldArchive)
            {
                WriteS3dTextures(s3dArchive, rootFolder + shortName, logger);
                return;
            }

            string wldFileName = archiveName + LanternStrings.WldFormatExtension;

            PfsFile wldFileInArchive = s3dArchive.GetFile(wldFileName);

            if (wldFileInArchive == null)
            {
                logger.LogError($"Unable to extract WLD file {wldFileName} from archive: {path}");
                return;
            }

            if (EqFileHelper.IsEquipmentArchive(archiveName))
            {
                ExtractArchiveEquipment(rootFolder, logger, settings, wldFileInArchive, shortName, s3dArchive);
            }
            else if (EqFileHelper.IsSkyArchive(archiveName))
            {
                ExtractArchiveSky(rootFolder, logger, settings, wldFileInArchive, shortName, s3dArchive);
            }
            else if (EqFileHelper.IsCharactersArchive(archiveName))
            {
                ExtractArchiveCharacters(path, rootFolder, logger, settings, archiveName, wldFileInArchive, shortName, s3dArchive);
            }
            else if (EqFileHelper.IsObjectsArchive(archiveName))
            {
                ExtractArchiveObjects(rootFolder, logger, settings, wldFileInArchive, shortName, s3dArchive);
            }
            else
            {
                ExtractArchiveZone(path, rootFolder, logger, settings, shortName, wldFileInArchive, s3dArchive);
            }

            MissingTextureFixer.Fix(archiveName);

        }

        public static void InitializeSharedCharacterWld(string rootFolder, ILogger logger, Settings settings)
        {
            var globalChrFileIndices = new List<string>() { "2", "3", "4", "" };
            var injectibleGlobalChrWlds = new List<WldFile>();

            foreach (var fileIndex in globalChrFileIndices)
            {
                var globalChrName = $"global{fileIndex}_chr";
                var globalChrS3d = Path.Combine(settings.EverQuestDirectory, $"{globalChrName}.s3d");

                var s3dArchive = new PfsArchive(globalChrS3d, logger);

                if (!s3dArchive.Initialize())
                {
                    logger.LogError("LanternExtractor: Failed to initialize PFS archive at path: " + globalChrS3d);
                    return;
                }

                var wldFileName = globalChrName + LanternStrings.WldFormatExtension;
                var wldFileInArchive = s3dArchive.GetFile(wldFileName);
                if (wldFileInArchive == null)
                {
                    logger.LogError($"Unable to extract WLD file {wldFileName} from archive: {globalChrS3d}");
                    return;
                }

                if (fileIndex != "")
                {
                    var injectibleChrWld = new WldFileCharacters(wldFileInArchive, globalChrName, WldType.Characters, logger, settings);
                    injectibleChrWld.Initialize(rootFolder, false);
                    injectibleGlobalChrWlds.Add(injectibleChrWld);
                }
                else
                {
                    GlobalReference.InitCharacterWld(s3dArchive, wldFileInArchive, rootFolder, "global", WldType.Characters, logger, settings, injectibleGlobalChrWlds);
                }
            }
        }

        private static void ExtractArchiveZone(string path, string rootFolder, ILogger logger, Settings settings,
            string shortName, PfsFile wldFileInArchive, PfsArchive s3dArchive)
        {
            // Some Kunark zones have a "_lit" which needs to be injected into the main zone file
            var s3dArchiveLit = new PfsArchive(path.Replace(shortName, shortName + "_lit"), logger);
            WldFileZone wldFileLit = null;

            if (s3dArchiveLit.Initialize())
            {
                var litWldFileInArchive = s3dArchiveLit.GetFile(shortName + "_lit.wld");
                wldFileLit = new WldFileZone(litWldFileInArchive, shortName, WldType.Zone,
                    logger, settings);
                wldFileLit.Initialize(rootFolder, false);

                var litWldLightsFileInArchive = s3dArchiveLit.GetFile(shortName + "_lit.wld");

                if (litWldLightsFileInArchive != null)
                {
                    var lightsWldFile =
                        new WldFileLights(litWldLightsFileInArchive, shortName, WldType.Lights, logger, settings, wldFileLit);
                    lightsWldFile.Initialize(rootFolder);
                }
            }

            var wldFile = new WldFileZone(wldFileInArchive, shortName, WldType.Zone, logger, settings, wldFileLit);

            // If we're trying to merge zone objects, inject here rather than pass down the chain to pull out later
            if (settings.ExportZoneWithObjects)
            {
                wldFile.BasePath = path;
                wldFile.BaseS3DArchive = s3dArchive;
                wldFile.WldFileToInject = wldFileLit;
                wldFile.RootFolder = rootFolder;
                wldFile.ShortName = shortName;
            }
            InitializeWldAndWriteTextures(wldFile, rootFolder, rootFolder + shortName + "/Zone/Textures/",
                s3dArchive, settings, logger);

            PfsFile lightsFileInArchive = s3dArchive.GetFile("lights" + LanternStrings.WldFormatExtension);

            if (lightsFileInArchive != null)
            {
                var lightsWldFile =
                    new WldFileLights(lightsFileInArchive, shortName, WldType.Lights, logger, settings, wldFileLit);
                lightsWldFile.Initialize(rootFolder);
            }

            PfsFile zoneObjectsFileInArchive = s3dArchive.GetFile("objects" + LanternStrings.WldFormatExtension);

            if (zoneObjectsFileInArchive != null)
            {
                WldFileZoneObjects zoneObjectsWldFile = new WldFileZoneObjects(zoneObjectsFileInArchive, shortName,
                    WldType.ZoneObjects, logger, settings, wldFileLit);
                zoneObjectsWldFile.Initialize(rootFolder);
            }

            ExtractSoundData(shortName, rootFolder, settings);
        }

        private static void ExtractArchiveObjects(string rootFolder, ILogger logger, Settings settings,
            PfsFile wldFileInArchive, string shortName, PfsArchive s3dArchive)
        {
            var wldFile = new WldFileZone(wldFileInArchive, shortName, WldType.Objects, logger, settings);
            InitializeWldAndWriteTextures(wldFile, rootFolder, 
                rootFolder + ShortnameHelper.GetCorrectZoneShortname(shortName) + "/Objects/Textures/",
                s3dArchive, settings, logger);
        }

        private static void ExtractArchiveCharacters(string path, string rootFolder, ILogger logger, Settings settings,
            string archiveName, PfsFile wldFileInArchive, string shortName, PfsArchive s3dArchive)
        {
            WldFileCharacters wldFileToInject = null;

            // global3_chr contains just animations
            if (archiveName.StartsWith("global3_chr"))
            {
                var s3dArchive2 = new PfsArchive(path.Replace("global3_chr", "global_chr"), logger);

                if (!s3dArchive2.Initialize())
                {
                    logger.LogError("Failed to initialize PFS archive at path: " + path);
                    return;
                }

                PfsFile wldFileInArchive2 = s3dArchive2.GetFile("global_chr.wld");

                wldFileToInject = new WldFileCharacters(wldFileInArchive2, "global_chr", WldType.Characters,
                    logger, settings);
                wldFileToInject.Initialize(rootFolder, false);
            }

            var wldFile = new WldFileCharacters(wldFileInArchive, shortName, WldType.Characters,
                logger, settings, new List<WldFile>() { wldFileToInject });

            string exportPath = rootFolder + (settings.ExportCharactersToSingleFolder &&
                                              settings.ModelExportFormat == ModelExportFormat.Intermediate
                ? "characters/Textures/"
                : ShortnameHelper.GetCorrectZoneShortname(shortName) + "/Characters/Textures/");

            InitializeWldAndWriteTextures(wldFile, rootFolder, exportPath,
                s3dArchive, settings, logger);
        }

        private static void ExtractArchiveSky(string rootFolder, ILogger logger, Settings settings, PfsFile wldFileInArchive,
            string shortName, PfsArchive s3dArchive)
        {
            var wldFile = new WldFileZone(wldFileInArchive, shortName, WldType.Sky, logger, settings);
            InitializeWldAndWriteTextures(wldFile, rootFolder, rootFolder + shortName + "/Textures/",
                s3dArchive, settings, logger);
        }

        private static void ExtractArchiveEquipment(string rootFolder, ILogger logger, Settings settings,
            PfsFile wldFileInArchive, string shortName, PfsArchive s3dArchive)
        {
            var wldFile = new WldFileEquipment(wldFileInArchive, shortName, WldType.Equipment, logger, settings);
            var exportPath = rootFolder +
                (settings.ExportEquipmentToSingleFolder &&
                 settings.ModelExportFormat == ModelExportFormat.Intermediate
                    ? "equipment/Textures/"
                    : shortName + "/Textures/");

            InitializeWldAndWriteTextures(wldFile, rootFolder, exportPath, s3dArchive, settings, logger);
        }

        private static void InitializeWldAndWriteTextures(WldFile wldFile, string rootFolder, string texturePath,
            PfsArchive s3dArchive, Settings settings, ILogger logger)
        {
            if (settings.ModelExportFormat != ModelExportFormat.GlTF)
            {
                wldFile.Initialize(rootFolder);
                s3dArchive.FilenameChanges = wldFile.FilenameChanges;
                WriteWldTextures(s3dArchive, wldFile, texturePath, logger);
            }
            else // Exporting to GlTF requires that the texture images already be present 
            {
                wldFile.Initialize(rootFolder, false);
                s3dArchive.FilenameChanges = wldFile.FilenameChanges;
                WriteWldTextures(s3dArchive, wldFile, texturePath, logger);
                wldFile.ExportData();
            }
        }
        /// <summary>
        /// Writes textures from the PFS archive to disk, converting them to PNG
        /// </summary>
        /// <param name="s3dArchive"></param>
        /// <param name="filePath"></param>
        private static void WriteS3dTextures(PfsArchive s3dArchive, string filePath, ILogger logger)
        {
            var allFiles = s3dArchive.GetAllFiles();

            foreach (var file in allFiles)
            {
                if (file.Name.EndsWith(".bmp") || file.Name.EndsWith(".dds"))
                {
                    ImageWriter.WriteImageAsPng(file.Bytes, filePath, file.Name, false, logger);
                }
            }
        }

        /// <summary>
        /// Writes textures from the PFS archive to disk, handling masked materials from the WLD
        /// </summary>
        /// <param name="s3dArchive"></param>
        /// <param name="wldFile"></param>
        /// <param name="zoneName"></param>
        public static void WriteWldTextures(PfsArchive s3dArchive, WldFile wldFile, string zoneName, ILogger logger)
        {
            var allBitmaps = wldFile.GetAllBitmapNames();
            var maskedBitmaps = wldFile.GetMaskedBitmaps();

            foreach (var bitmap in allBitmaps)
            {
                string filename = bitmap;
                if (s3dArchive.FilenameChanges != null &&
                    s3dArchive.FilenameChanges.ContainsKey(Path.GetFileNameWithoutExtension(bitmap)))
                {
                    filename = s3dArchive.FilenameChanges[Path.GetFileNameWithoutExtension(bitmap)] + ".bmp";
                }

                var pfsFile = s3dArchive.GetFile(filename);

                if (pfsFile == null)
                {
                    continue;
                }

                bool isMasked = maskedBitmaps != null && maskedBitmaps.Contains(bitmap);
                ImageWriter.WriteImageAsPng(pfsFile.Bytes, zoneName, bitmap, isMasked, logger);
            }
        }

        private static void ExtractSoundData(string shortName, string rootFolder, Settings settings)
        {
            var sounds = new EffSndBnk(settings.EverQuestDirectory + shortName + "_sndbnk" +
                                       LanternStrings.SoundFormatExtension);
            sounds.Initialize();
            var soundEntries =
                new EffSounds(
                    settings.EverQuestDirectory + shortName + "_sounds" + LanternStrings.SoundFormatExtension, sounds);
            soundEntries.Initialize();
            soundEntries.ExportSoundData(shortName, rootFolder);
        }
    }
}