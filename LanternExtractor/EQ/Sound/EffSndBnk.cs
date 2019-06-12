using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;

namespace LanternExtractor.EQ.Sound
{
    /// <summary>
    /// The EffSndBnk files are plain text files ending with "_sndbnk." which list the sounds in each zone
    /// The indices of these sounds are then used by the sound entries to determine which sound to load
    /// </summary>
    public class EffSndBnk
    {
        /// <summary>
        /// A list containing all of the emit sounds for this zone
        /// </summary>
        public readonly List<string> EmitSounds = new List<string>();

        /// <summary>
        /// A list containing all of the looping sounds for this zone
        /// </summary>
        public readonly List<string> LoopSounds = new List<string>();

        /// <summary>
        /// The OS path to the sound file
        /// </summary>
        private readonly string _soundFilePath;

        /// <summary>
        /// Sets the reference to the sound bank file
        /// </summary>
        /// <param name="soundFilePath">The OS path to the sound bank file</param>
        public EffSndBnk(string soundFilePath)
        {
            _soundFilePath = soundFilePath;
        }

        /// <summary>
        /// Parses the sound bank file
        /// </summary>
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