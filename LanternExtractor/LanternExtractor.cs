using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LanternExtractor.EQ;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor
{
    static class LanternExtractor
    {
        private static Settings _settings;
        private static ILogger _logger;
        // Switch to true to use multiple processes for processing
        private static bool _useMultiProcess = false;

        // Batch jobs n at a time
        private static int _processCount = 4;
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "PROCESS_JOB")
            {
                var zoneFiles = args.Skip(1).ToArray();
                var scrubbedZoneFiles = zoneFiles.Select(s => Regex.Match(s, "(\\w+)(?:\\.s3d)$").ToString()).ToArray();

                _logger = new TextFileLogger($"log-{Process.GetCurrentProcess().Id}.txt");
                _logger.LogInfo(string.Join("-", scrubbedZoneFiles));
                _settings = new Settings("settings.txt", _logger);

                foreach (var fileName in zoneFiles)
                {
                    Console.WriteLine($"Started extracting {fileName}");
                    ArchiveExtractor.Extract(fileName, "Exports/", _logger, _settings);
                    Console.WriteLine($"Finished extracting {fileName}");
                }
                return;
            }


            _logger = new TextFileLogger("log.txt");
            _settings = new Settings("settings.txt", _logger);
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
                Console.WriteLine("No valid EQ files found for: '" + archiveName + "' at path: " +
                                  _settings.EverQuestDirectory);
                return;
            }

            if (_useMultiProcess && _processCount > 0)
            {
                List<Task> tasks = new List<Task>();
                int i = 0;

                // Each process is responsible for n number of files to work through determined by the process count here.
                int chunkCount = Math.Max(1, (int)Math.Ceiling((double)(eqFiles.Count / _processCount)));
                foreach (var chunk in eqFiles.GroupBy(s => i++ / chunkCount).Select(g => g.ToArray()).ToArray())
                {
                    Task task = Task.Factory.StartNew(() =>
                    {
                        var processJob = Process.Start("LanternExtractor.exe", string.Join(" ", chunk.Select(c => $"\"{c}\"").ToArray().Prepend("PROCESS_JOB")));
                        processJob.WaitForExit();
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
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
        }
    }
}
