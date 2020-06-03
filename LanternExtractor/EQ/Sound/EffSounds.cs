using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LanternExtractor.EQ.Sound
{
    /// <summary>
    /// The EffSound file is binary binary ending with "_sounds.eff" extension.
    /// It contains no header, just an array of entries each consisting of 84 bytes.
    /// Each entry describes an instance of a sound or music in the world.
    /// </summary>
    public class EffSounds
    {
        /// <summary>
        /// The sound bank referenced for sound names
        /// </summary>
        private readonly EffSndBnk _soundBank;
        
        private readonly string _soundFilePath;
        
        private readonly List<SoundEntry> _soundEntries = new List<SoundEntry>();
        
        private readonly List<string> _musicTrackEntries = new List<string>();
        
        private const int EntryLengthInBytes = 84;
        
        public EffSounds(string soundFilePath, EffSndBnk soundBank)
        {
            _soundFilePath = soundFilePath;
            _soundBank = soundBank;
        }
        
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
                    newSound.SoundIdDay = GetMusicTrackName(soundId1);
                    newSound.SoundIdNight = GetMusicTrackName(soundId1);
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
        
        private void LoadMusicTrackNames(string zoneShortName)
        {
            string[] trackLines = File.ReadAllLines("musictracks.txt");

            bool isTargetZone = false;
            
            foreach (string line in trackLines)
            {
                if (!isTargetZone)
                {
                    if (line == "#" + zoneShortName)
                    {
                        isTargetZone = true;
                    }

                    continue;
                }

                if (line == string.Empty)
                {
                    break;
                }

                _musicTrackEntries.Add(line);
            }
        }

        /// <summary>
        /// Returns the name of the sound based on either the internal reference or the sound back definitions
        /// The client uses specific integer ranges to identify the type.
        /// There are also a handful of hardcoded sound ids.
        /// </summary>
        /// <param name="soundId">The id index of the sound</param>
        /// <param name="soundBank">The sound bank in which to look</param>
        /// <param name="soundType">The emission type</param>
        /// <returns>The name of the sound</returns>
        private string GetSoundString(int soundId, EffSndBnk soundBank, ref EmissionType soundType)
        {
            if (soundId == 0)
            {
                return string.Empty;
            }

            if (soundId >= 1 && soundId < 32 && soundId < soundBank.EmitSounds.Count)
            {
                soundType = EmissionType.Emit;
                return soundBank.EmitSounds[soundId - 1];
            }

            // Hardcoded client sounds - verified that no other references exist in Trilogy client
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

            if (soundId < 162 || soundId >= 162 + soundBank.LoopSounds.Count)
            {
                return string.Empty;
            }
            
            soundType = EmissionType.Loop;

            return soundBank.LoopSounds[soundId - 161 - 1];
        }
        
        public void ExportSoundData(string zoneName)
        {
            var soundExport = new StringBuilder();
            var musicExport = new StringBuilder();

            foreach (SoundEntry entry in _soundEntries)
            {
                if (entry.SoundType == SoundType.Music)
                {
                    musicExport.Append(entry.PosX);
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.PosZ);
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.PosY);
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.Radius);
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.SoundIdDay);
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.SoundIdNight);
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.AsDistance); // Day loop count
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.UnkRange64); // Night loop count
                    musicExport.Append(LanternStrings.TextExportSeparator);
                    musicExport.Append(entry.FadeOutMs);
                    musicExport.AppendLine();
                }
                else
                {
                    soundExport.Append((int)entry.SoundType);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.PosX);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.PosZ);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.PosY);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.Radius);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.SoundIdDay);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.SoundIdNight);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.CooldownDay);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.CooldownNight);
                    soundExport.Append(LanternStrings.TextExportSeparator);
                    soundExport.Append(entry.RandomDelay);
                    soundExport.AppendLine();
                }
            }

            if (soundExport.Length != 0)
            {
                StringBuilder exportHeader = new StringBuilder();
                exportHeader.AppendLine(LanternStrings.ExportHeaderTitle + "Sound Instances");
                exportHeader.AppendLine("# Format: SoundType, PosX, PosY, PosZ, Radius, SoundIdDay, SoundIdNight, CooldownDay, CooldownNight, RandomDelay");

                Directory.CreateDirectory(zoneName + "/");
                File.WriteAllText(zoneName + "/sounds.txt", exportHeader.ToString() + soundExport);
            }

            if (musicExport.Length != 0)
            {
                StringBuilder exportHeader = new StringBuilder();
                exportHeader.AppendLine(LanternStrings.ExportHeaderTitle + "Music Instances");
                exportHeader.AppendLine("# Format: PosX, PosY, PosZ, Radius, SoundIdDay, SoundIdNight, DayLoopCount, NightLoopCount, FadeOutMs");

                Directory.CreateDirectory(zoneName + "/");
                File.WriteAllText(zoneName + "/music.txt", exportHeader.ToString() + musicExport);
            }
        }

        /// <summary>
        /// Gets the name of the music track if it exists
        /// </summary>
        /// <param name="zoneName">The zone shortname</param>
        /// <param name="index">The index of the track</param>
        /// <returns>The name of the track</returns>
        private string GetMusicTrackName(int index)
        {
            if (index < 0 || index >= _musicTrackEntries.Count)
            {
                return "Unknown";
            }
            
            return _musicTrackEntries[index];
        }
    }
}