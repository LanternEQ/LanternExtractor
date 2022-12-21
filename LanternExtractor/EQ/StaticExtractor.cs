using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ
{
    public static class StaticExtractor
    {
        public const string StaticDirectory = "static";

        public static void Extract(string archiveName, string rootFolder, ILogger logger, Settings settings)
        {
            if (settings.ExportStaticFiles == null ||
                settings.ModelExportFormat != ModelExportFormat.Intermediate ||
                IsInvalidName(archiveName))
            {
                return;
            }

            WriteAllFiles(rootFolder, logger, settings);
        }

        private static void WriteAllFiles(string rootFolder, ILogger logger, Settings settings)
        {
            Directory.CreateDirectory(rootFolder + StaticDirectory);

            var filePaths = GetStaticFilePaths(settings);

            foreach (var filePath in filePaths)
            {
                var destFilePath = GetDestinationPath(rootFolder, filePath);
                logger.LogInfo($"Copying {filePath} to {destFilePath}");
                File.Copy(filePath, destFilePath, true);
            }
        }

        private static List<string> GetStaticFilePaths(Settings settings)
        {
            var paths = new List<string>();

            foreach (var filePath in settings.ExportStaticFiles.Split(','))
            {
                var staticFilePath = Path.Combine(settings.EverQuestDirectory, filePath.Trim());
                if (File.Exists(staticFilePath))
                {
                    paths.Add(staticFilePath);
                }
            }

            return paths;
        }

        private static string GetDestinationPath(string rootFolder, string sourceFilePath)
        {
            var sourceFileName = Path.GetFileName(sourceFilePath);
            return Path.Combine(rootFolder + StaticDirectory, sourceFileName);
        }

        private static bool IsInvalidName(string archiveName)
        {
            return !EqFileHelper.IsStaticArchive(archiveName) && archiveName != "all";
        }
    }
}
