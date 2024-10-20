using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanternExtractor.EQ;
using LanternExtractor.Infrastructure.Logger;
using LanternExtractor.Infrastructure.Settings;
using LanternExtractor.Performance;

namespace LanternExtractor
{
    static class LanternExtractor
    {
        private static Settings _settings;
        private static ILogger _logger;
        private static bool _useMultithreading = false;

        private static void Main(string[] args)
        {
            _logger = new TextFileLogger("log.txt");
            _settings = new Settings("settings.toml", _logger);
            _settings.Initialize();
            _logger.SetVerbosity((LogVerbosity)_settings.LoggerVerbosity);

            DateTime start = DateTime.Now;

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: lantern.exe <filename/shortname/all>");
                return;
            }

            var archiveName = args[0];
            List<string> eqFiles = EqFileHelper.GetValidEqFilePaths(_settings.EverQuestDirectory, archiveName);
            eqFiles.Sort();

            if (eqFiles.Count == 0 && !EqFileHelper.IsSpecialCaseExtraction(archiveName))
            {
                Console.WriteLine($"No valid EQ files found for: '{archiveName}' at path: {_settings.EverQuestDirectory}");
                return;
            }

            if (_useMultithreading)
            {
                int availableCores = Environment.ProcessorCount;
                Console.WriteLine($"Multithreading enabled with {availableCores} threads.");

                Parallel.ForEach(eqFiles, new ParallelOptions { MaxDegreeOfParallelism = availableCores }, file =>
                {
                    ArchiveExtractor.Extract(file, "Exports/", _logger, _settings);
                });
            }
            else
            {
                foreach (var file in eqFiles)
                {
                    ArchiveExtractor.Extract(file, "Exports/", _logger, _settings);
                }
            }

            ClientDataCopier.Copy(archiveName, "Exports/", _logger, _settings);
            MusicCopier.Copy(archiveName, _logger, _settings);

            Console.WriteLine($"Extraction complete ({(DateTime.Now - start).TotalSeconds:.00}s)");
            Console.WriteLine(Benchmarker.ReportAverageTimes());
        }
    }
}
