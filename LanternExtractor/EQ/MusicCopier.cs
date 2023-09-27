using System.IO;
using System.Linq;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ
{
    public static class MusicCopier
    {
        private const string MusicDirectory = "music";

        public static void Copy(string shortname, ILogger logger, Settings settings)
        {
            if (shortname != "music" && shortname != "all")
            {
                return;
            }
            
            if (!settings.CopyMusic)
            {
                return;
            }

            var xmiFiles = Directory.GetFiles(settings.EverQuestDirectory, "*.*", SearchOption.AllDirectories)
                .Where(EqFileHelper.IsMusicFile).ToList();
            var destinationFolder = "Exports/" + MusicDirectory;

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            foreach (var xmi in xmiFiles)
            {
                var fileName = Path.GetFileName(xmi);
                var destination = Path.Combine(destinationFolder, fileName);
                if (File.Exists(destination))
                {
                    continue;
                }

                File.Copy(xmi, destination);
            }
        }
    }
}