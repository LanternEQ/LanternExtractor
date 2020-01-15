using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;

namespace LanternExtractor.EQ.Sound
{
    /// <summary>
    /// EffSndBnk files are plain text files ending with "_sndbnk" which list the sounds in each zone.
    /// The indices of these sounds are used by the sound entries to determine which sound should play.
    /// </summary>
    public class EffSndBnk
    {
        public readonly List<string> EmitSounds = new List<string>();
        
        public readonly List<string> LoopSounds = new List<string>();
        
        private readonly string _soundFilePath;
        
        public EffSndBnk(string soundFilePath)
        {
            _soundFilePath = soundFilePath;
        }
        
        public void Initialize()
        {
            List<string> currentList = null;

            if (!File.Exists(_soundFilePath))
            {
                return;
            }

            string fileText = File.ReadAllText(_soundFilePath);

            List<string> parsedLines = TextParser.ParseTextByNewline(fileText);

            if (parsedLines == null || parsedLines.Count == 0)
            {
                return;
            }

            foreach (var line in parsedLines)
            {
                switch (line)
                {
                    case "":
                        continue;
                    case "EMIT":
                        currentList = EmitSounds;
                        continue;
                    case "LOOP":
                        currentList = LoopSounds;
                        continue;
                    default:
                        currentList?.Add(line);
                        break;
                }
            }
        }
    }
}