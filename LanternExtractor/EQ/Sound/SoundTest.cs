using System.IO;
using System.Linq;

namespace LanternExtractor.EQ.Sound
{
    /// <summary>
    /// A small helper class useful for sound isolation and testing
    /// </summary>
    public static class SoundTest
    {
        public static void OutputSingleInstance(BinaryWriter writer, int index, string fileName)
        {
            var memoryStream = new MemoryStream();
            writer.BaseStream.CopyTo(memoryStream);
            File.WriteAllBytes(fileName,
                memoryStream.ToArray().Skip(index * EffSounds.EntryLengthInBytes).Take(EffSounds.EntryLengthInBytes).ToArray());
        }
        
        public static void ModifyInstance(BinaryWriter writer, int index, string fileName)
        {
            writer.BaseStream.Position = 16; // positions
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(50f);
            
            var memoryStream = new MemoryStream();
            writer.BaseStream.CopyTo(memoryStream);
            File.WriteAllBytes(fileName, memoryStream.ToArray());
        }
    }
}