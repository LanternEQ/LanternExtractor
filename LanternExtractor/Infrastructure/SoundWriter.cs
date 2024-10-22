using System.IO;
using Serilog;

namespace LanternExtractor.Infrastructure
{
    public static class SoundWriter
    {
        public static void WriteSoundAsWav(byte[] bytes, string filePath, string fileName)
        {
            Directory.CreateDirectory(filePath);
            var path = Path.Combine(filePath, fileName);

            if (File.Exists(path))
            {
                Log.Information($"SoundWriter: overwriting {fileName}");
            }

            File.WriteAllBytes(path, bytes);
        }
    }
}
