using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanternExtractor.Infrastructure;

namespace LanternExtractor.EQ.Sound
{
    /// <summary>
    /// The EffSound file is binary data ending with "_sounds.eff"
    /// It contains no header, just an array of entries each consisting of 84 bytes
    /// Each entry describes an instance of a sound or music in the world
    /// </summary>
    public class EffSounds
    {
        /// <summary>
        /// The sound bank referenced for sound names
        /// </summary>
        private readonly EffSndBnk _soundBank;

        /// <summary>
        /// The OS path to the sound file
        /// </summary>
        private readonly string _soundFilePath;

        /// <summary>
        /// The sound entries for this zone
        /// </summary>
        private readonly List<SoundEntry> _soundEntries = new List<SoundEntry>();

        /// <summary>
        /// The music track entries - read in from musictracks.txt
        /// </summary>
        private readonly Dictionary<string, string> _musicTrackEntries = new Dictionary<string, string>();

        /// <summary>
        /// The size of the sound entry in bytes
        /// </summary>
        private const int EntryLengthInBytes = 84;

        /// <summary>
        /// Sets the path and sound bank reference
        /// </summary>
        /// <param name="soundFilePath">The OS path</param>
        /// <param name="soundBank">A reference to the sound bank</param>
        public EffSounds(string soundFilePath, EffSndBnk soundBank)
        {
            _soundFilePath = soundFilePath;
            _soundBank = soundBank;
        }

        /// <summary>
        /// Initializes the sound entry list
        /// </summary>
        public void Initialize()
        {
            if (_soundBank == null || !File.Exists(_soundFilePath))
            {
                return;
            }

            FileStream file = File.Open(_soundFilePath, FileMode.Open);

            string zoneShortName = Path.GetFileNameWithoutExtension(_soundFilePath).Split('_')[0];

            LoadMusicTrackNames(zoneShortName);

            var reader = new BinaryReader(file);

            int fileLength = (int) reader.BaseStream.Length;

            if (fileLength % EntryLengthInBytes != 0)
            {
                return;
            }

            int entryCount = fileLength / EntryLengthInBytes;

            for (int i = 0; i < entryCount; ++i)
            {
                var newSound = new SoundEntry();
                newSound.UnkRef00 = reader.ReadInt32();
                newSound.UnkRef04 = reader.ReadInt32();
                newSound.Reserved = reader.ReadInt32();
                newSound.Sequence = reader.ReadInt32();

                newSound.PosX = reader.ReadSingle();
                newSound.PosY = reader.ReadSingle();
                newSound.PosZ = reader.ReadSingle();
                newSound.Radius = reader.ReadSingle();

                newSound.CooldownDay = reader.ReadInt32();
                newSound.CooldownNight = reader.ReadInt32();
                newSound.RandomDelay = reader.ReadInt32();

                newSound.Unk44 = reader.ReadInt32();
                int soundId1 = reader.ReadInt32();
                int soundId2 = reader.ReadInt32();

                byte soundType = reader.ReadByte();
                newSound.SoundType = (SoundType) soundType;

                if (soundType == 0 || soundType == 2 || soundType == 3)
                {
                    // Find the sound names
                    EmissionType newSoundEmissionType = newSound.EmissionType;
                    newSound.SoundIdDay = GetSoundString(soundId1, _soundBank, ref newSoundEmissionType);
                    EmissionType soundEmissionType = newSound.EmissionType;
                    newSound.SoundIdNight = GetSoundString(soundId2, _soundBank, ref soundEmissionType);
                }
                else
                {
                    newSound.SoundIdDay = GetMusicTrackName(zoneShortName, soundId1);
                    newSound.SoundIdNight = GetMusicTrackName(zoneShortName, soundId1);
                }

                newSound.UnkPad57 = reader.ReadByte();
                newSound.UnkPad58 = reader.ReadByte();
                newSound.UnkPad59 = reader.ReadByte();

                newSound.AsDistance = reader.ReadInt32();
                newSound.UnkRange64 = reader.ReadInt32();
                newSound.FadeOutMs = reader.ReadInt32();
                newSound.UnkRange72 = reader.ReadInt32();
                newSound.FullVolRange = reader.ReadInt32();
                newSound.UnkRange80 = reader.ReadInt32();

                if (newSound.SoundIdDay != "" || newSound.SoundIdNight != "")
                {
                    _soundEntries.Add(newSound);
                }
            }
        }

        /// <summary>
        /// Loads the list of track names specific to each zone
        /// </summary>
        /// <param name="zoneShortName">The zone shortname</param>
        private void LoadMusicTrackNames(string zoneShortName)
        {
            string trackContents = File.ReadAllText("musictracks.txt");

            var splitZones = TextParser.ParseTextByEmptyLines(trackContents);

            foreach (string entry in splitZones)
            {
                // Split this into lines
                var trackEntries = TextParser.ParseTextByNewline(entry);

                if (trackEntries[0] != zoneShortName)
                {
                    continue;
                }

                for (int i = 1; i < trackEntries.Count; ++i)
                {
                    var trackDetails = trackEntries[i].Split(',');

                    if (trackDetails.Length != 3)
                    {
                        continue;
                    }

                    _musicTrackEntries[zoneShortName + trackDetails[0]] = trackDetails[1];
                }
            }
        }

        /// <summary>
        /// Returns the name of the sound based on either the internal reference or the sound back definitions
        /// </summary>
        /// <param name="soundId">The id index of the sound</param>
        /// <param name="soundBank">The sound bank in which to look</param>
        /// <param name="soundType">The emission type</param>
        /// <returns>The name of the sound</returns>
        private string GetSoundString(int soundId, EffSndBnk soundBank, ref EmissionType soundType)
        {
            if (soundId == 0)
            {
                return "";
            }

            // Emit sounds
            if (soundId >= 1 && soundId < 32 && soundId < soundBank.EmitSounds.Count)
            {
                soundType = EmissionType.Emit;
                return soundBank.EmitSounds[soundId - 1];
            }

            // Hardcoded client sounds
            if (soundId >= 32 && soundId < 162)
            {
                soundType = EmissionType.Internal;

                switch (soundId)
                {
                    case 39:
                        return "death_me";
                    case 143:
                        return "thunder1";
                    case 144:
                        return "thunder2";
                    case 158:
                        return "wind_lp1";
                    case 159:
                        return "rainloop";
                    case 160:
                        return "torch_lp";
                    case 161:
                        return "watundlp";
                }
            }

            // Loop sounds
            if (soundId < 162 || soundId >= 162 + soundBank.LoopSounds.Count)
            {
                return "";
            }
            
            soundType = EmissionType.Loop;

            return soundBank.LoopSounds[soundId - 161 - 1];

        }

        /// <summary>
        /// Writes sound data to a plaintext file
        /// </summary>
        /// <param name="zoneName">The shortname of the zone - used for naming the file</param>
        public void ExportSoundData(string zoneName)
        {
            var soundExport = new StringBuilder();
            var musicExport = new StringBuilder();

            foreach (SoundEntry entry in _soundEntries)
            {
                if (entry.SoundType == SoundType.Music)
                {
                    musicExport.Append(entry.SoundIdDay + "," + entry.SoundIdNight + ","
                                       + entry.PosX + "," + entry.PosZ + "," + entry.PosY + "," +
                                       entry.Radius + ","
                                       + entry.FadeOutMs + "\n");
                }
                else
                {
                    soundExport.Append(entry.SoundIdDay + "," + entry.SoundIdNight + ","
                                       + Convert.ToInt32(entry.SoundType) + "," +
                                       Convert.ToInt32(entry.EmissionType) + ","
                                       + entry.PosX + "," + entry.PosZ + "," + entry.PosY + "," +
                                       entry.Radius + ","
                                       + entry.CooldownDay + "," + entry.CooldownNight + "," +
                                       entry.RandomDelay + "\n");
                }
            }

            if (soundExport.Length != 0)
            {
                string exportHeader = LanternStrings.ExportHeaderTitle + "Sound Instances\n";
                exportHeader += "# Format: SoundIdDay, SoundIdNight, SoundType, EmissionType, PosX, PosY, PosZ, Radius, CooldownDay, CooldownNight, RandomDelay\n";

                File.WriteAllText(zoneName + "/" + zoneName + "_sounds.txt", exportHeader + soundExport);
            }

            if (musicExport.Length != 0)
            {
                string exportHeader = LanternStrings.ExportHeaderTitle + "Music Instances\n";
                exportHeader += "# Format: SoundIdDay, SoundIdNight, PosX, PosY, PosZ, Radius, FadeOutMs\n";

                File.WriteAllText(zoneName + "/" + zoneName + "_music.txt", exportHeader + musicExport);
            }
        }

        /// <summary>
        /// Gets the name of the music track if it exists
        /// </summary>
        /// <param name="zoneName">The zone shortname</param>
        /// <param name="index">The index of the track</param>
        /// <returns>The name of the track</returns>
        private string GetMusicTrackName(string zoneName, int index)
        {
            return !_musicTrackEntries.ContainsKey(zoneName + index)
                ? "UnknownMusic"
                : _musicTrackEntries[zoneName + index];
        }
    }
}