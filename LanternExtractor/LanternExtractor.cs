using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LanternExtractor.EQ;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Settings;
using Serilog.Events;

namespace LanternExtractor
{
    static class LanternExtractor
    {
        private static Settings _settings;

        private static void Main(string[] args)
        {
            LogHelper.InitializeLogging(LogEventLevel.Verbose);
            InitializeSettings();
            LogHelper.SetLogLevel(_settings.LoggerVerbosity);

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: lantern.exe <filename/shortname/all>");
                return;
            }

            var archiveName = args[0];
            List<string> eqFiles = EqFileHelper.GetValidEqFilePaths(_settings.EverQuestDirectory, archiveName);
            if (eqFiles.Count == 0 && !EqFileHelper.IsSpecialCaseExtraction(archiveName))
            {
                Console.WriteLine($"No valid EQ files found for: '{archiveName}' at path: {_settings.EverQuestDirectory}");
                return;
            }

            ExtractFiles(archiveName, eqFiles);
        }

        private static void InitializeSettings()
        {
            _settings = new Settings("settings.toml");
            _settings.Initialize();
        }

        private static void ExtractFiles(string archiveName, List<string> eqFiles)
        {
            bool useMultithreading = _settings.UseMultithreading;
            int processorCount = Environment.ProcessorCount;
            Console.WriteLine(useMultithreading
                ? $"Multithreading enabled ({processorCount} processors)"
                : "Multithreading disabled");

            var progressBar = new ProgressBar(eqFiles.Count, '*', useMultithreading, '*');

            if (useMultithreading)
            {
                Parallel.ForEach(eqFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
                {
                    ArchiveExtractor.Extract(file, "Exports/", _settings);
                    progressBar.Step(Path.GetFileName(file));
                });
            }
            else
            {
                foreach (var file in eqFiles)
                {
                    progressBar.Step(Path.GetFileName(file));
                    ArchiveExtractor.Extract(file, "Exports/", _settings);
                }
                progressBar.Step(string.Empty);
            }

            ClientDataCopier.Copy(archiveName, "Exports/", _settings);
            MusicCopier.Copy(archiveName, _settings);
        }
    }
}
