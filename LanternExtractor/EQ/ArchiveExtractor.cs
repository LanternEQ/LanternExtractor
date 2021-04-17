using System.IO;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Sound;
using LanternExtractor.EQ.Wld;
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
                var wldFile = new WldFileEquipment(wldFileInArchive, shortName, WldType.Equipment, logger, settings);
                wldFile.Initialize(rootFolder);
                WriteWldTextures(s3dArchive, wldFile, rootFolder + "/equipment/Textures/", logger);
            }
            else if (EqFileHelper.IsSkyArchive(archiveName))
            {
                var wldFile = new WldFileZone(wldFileInArchive, shortName, WldType.Sky, logger, settings);
                wldFile.Initialize(rootFolder);
                WriteWldTextures(s3dArchive, wldFile, rootFolder + shortName + "/Textures/", logger);
            }
            else if (EqFileHelper.IsCharactersArchive(archiveName))
            {
                WldFileCharacters wldFileToInject = null;

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
                    logger, settings, wldFileToInject);
                wldFile.Initialize(rootFolder);

                string exportPath = rootFolder + (settings.ExportCharactersToSingleFolder
                    ? "characters/Characters/Textures/"
                    : shortName + "/Characters/Textures/");

                s3dArchive.FilenameChanges = wldFile.FilenameChanges;
                WriteWldTextures(s3dArchive, wldFile, exportPath, logger);
            }
            else if (EqFileHelper.IsObjectsArchive(archiveName))
            {
                var wldFile = new WldFileObjects(wldFileInArchive, shortName, WldType.Objects, logger, settings);
                wldFile.Initialize(rootFolder);
                WriteWldTextures(s3dArchive, wldFile, rootFolder + shortName + "/Objects/Textures/", logger);
            }
            else
            {
                var wldFile = new WldFileZone(wldFileInArchive, shortName, WldType.Zone, logger, settings);
                wldFile.Initialize(rootFolder);
                WriteWldTextures(s3dArchive, wldFile, rootFolder + shortName + "/Zone/Textures/", logger);

                PfsFile lightsFileInArchive = s3dArchive.GetFile("lights" + LanternStrings.WldFormatExtension);

                if (lightsFileInArchive != null)
                {
                    var lightsWldFile =
                        new WldFileLights(lightsFileInArchive, shortName, WldType.Lights, logger, settings);
                    lightsWldFile.Initialize(rootFolder);
                }

                PfsFile zoneObjectsFileInArchive = s3dArchive.GetFile("objects" + LanternStrings.WldFormatExtension);

                if (zoneObjectsFileInArchive != null)
                {
                    WldFileZoneObjects zoneObjectsWldFile = new WldFileZoneObjects(zoneObjectsFileInArchive, shortName,
                        WldType.ZoneObjects, logger, settings);
                    zoneObjectsWldFile.Initialize(rootFolder);
                }

                ExtractSoundData(shortName, rootFolder, settings);
            }
        }

        /// <summary>
        /// Writes textures from the PFS archive to disk, converting them to PNG
        /// </summary>
        /// <param name="s3dArchive"></param>
        /// <param name="zoneName"></param>
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
        private static void WriteWldTextures(PfsArchive s3dArchive, WldFile wldFile, string zoneName, ILogger logger)
        {
            var allBitmaps = wldFile.GetAllBitmapNames();
            var maskedBitmaps = wldFile.GetMaskedBitmaps();

            foreach (var bitmap in allBitmaps)
            {
                var pfsFile = s3dArchive.GetFile(bitmap);

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