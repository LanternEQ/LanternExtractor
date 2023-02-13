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
            int fileLength = (int)reader.BaseStream.Length;

            if (fileLength % EntryLengthInBytes != 0)
            {
                // File is an incorrect size
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
                newSound.SoundType = (SoundType)soundType;

                if (soundType == 0 || soundType == 2 || soundType == 3)
                {
                    // Find the sound names
                    EmissionType newSoundEmissionType = newSound.EmissionType;
                    newSound.SoundIdDay = soundId1; //GetSoundString(soundId1, _soundBank, ref newSoundEmissionType);
                    EmissionType soundEmissionType = newSound.EmissionType;
                    newSound.SoundIdNight = soundId2; //GetSoundString(soundId2, _soundBank, ref soundEmissionType);
                }
                else
                {
                    newSound.SoundIdDay = soundId1; //GetMusicTrackName(soundId1);
                    newSound.SoundIdNight = soundId2; //GetMusicTrackName(soundId1);
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

                //if (newSound.SoundIdDay != "" || newSound.SoundIdNight != "")
                {
                    _soundEntries.Add(newSound);
                }
            }
        }

        private void LoadMusicTrackNames(string zoneShortName)
        {
            string[] trackLines = File.ReadAllLines("ClientData/musictracks.txt");

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
        /// <param name="soundIdNight"></param>
        /// <param name="soundBank">The sound bank in which to look</param>
        /// <param name="soundNameNight"></param>
        /// <param name="soundType">The emission type</param>
        /// <param name="soundIdDay"></param>
        /// <param name="soundNameDay"></param>
        /// <returns>The name of the sound</returns>
        private bool TryGetSoundInfo(int soundIdDay, int soundIdNight, EffSndBnk soundBank, out string soundNameDay,
            out string soundNameNight)
        {
            var typeDay = GetEmissionType(soundIdDay);
            var typeNight = GetEmissionType(soundIdNight);
            soundNameDay = string.Empty;
            soundNameNight = string.Empty;

            if (typeDay == EmissionType.None && typeNight == EmissionType.None)
            {
                // No sound
                return false;
            }

            if (typeDay != typeNight)
            {
                // Two separate emission types
                return false;
            }

            if (typeDay != EmissionType.None)
            {
                soundNameDay = GetSoundName(soundIdDay, typeDay);
            }

            if (typeNight != EmissionType.None)
            {
                soundNameNight = GetSoundName(soundIdDay, typeDay);
            }

            return true;
        }

        private string GetSoundName(int soundId, EmissionType type)
        {
            switch (type)
            {
                case EmissionType.Emit:
                    return _soundBank.GetEmitSound(soundId - 1);
                case EmissionType.Loop:
                    return _soundBank.GetLoopSound(soundId - 162);
                case EmissionType.Internal:
                    return ClientSounds.GetClientSound(soundId);
                default:
                    return SoundConstants.Unknown;
            }
        }

        private EmissionType GetEmissionType(int soundId)
        {
            if (soundId <= 0)
            {
                return EmissionType.None;
            }

            if (soundId < 32)
            {
                return EmissionType.Emit;
            }

            return soundId < 162 ? EmissionType.Internal : EmissionType.Loop;
        }

        public void ExportSoundData(string zoneName, string rootFolder)
        {
            var soundExport = new StringBuilder();
            var musicExport = new StringBuilder();

            foreach (var entry in _soundEntries)
            {
                if (entry.SoundType == SoundType.Music)
                {
                    musicExport.Append(entry.PosX);
                    musicExport.Append(",");
                    musicExport.Append(entry.PosZ);
                    musicExport.Append(",");
                    musicExport.Append(entry.PosY);
                    musicExport.Append(",");
                    musicExport.Append(entry.Radius);
                    musicExport.Append(",");
                    musicExport.Append(entry.SoundIdDay);
                    musicExport.Append(",");
                    musicExport.Append(entry.SoundIdNight);
                    musicExport.Append(",");
                    musicExport.Append(entry.AsDistance); // Day loop count
                    musicExport.Append(",");
                    musicExport.Append(entry.UnkRange64); // Night loop count
                    musicExport.Append(",");
                    musicExport.Append(entry.FadeOutMs);
                    musicExport.AppendLine();
                }
                else
                {
                    if (!TryGetSoundInfo(entry.SoundIdDay, entry.SoundIdNight, _soundBank, out var soundNameDay,
                        out var soundNameNight, out var emissionType))
                    {
                        continue;
                    }

                    soundExport.Append((int)entry.SoundType);
                    soundExport.Append(",");
                    soundExport.Append(entry.PosX);
                    soundExport.Append(",");
                    soundExport.Append(entry.PosZ);
                    soundExport.Append(",");
                    soundExport.Append(entry.PosY);
                    soundExport.Append(",");
                    soundExport.Append(entry.Radius);
                    soundExport.Append(",");
                    soundExport.Append(soundNameDay);
                    soundExport.Append(",");
                    soundExport.Append(soundNameNight);
                    soundExport.Append(",");
                    soundExport.Append(entry.CooldownDay);
                    soundExport.Append(",");
                    soundExport.Append(entry.CooldownNight);
                    soundExport.Append(",");
                    soundExport.Append(entry.RandomDelay);
                    soundExport.AppendLine();
                }
            }

            string exportPath = Path.Combine(rootFolder, zoneName, "Zone/");

            if (soundExport.Length > 0)
            {
                StringBuilder exportHeader = new StringBuilder();
                exportHeader.AppendLine(LanternStrings.ExportHeaderTitle + "Sound Instances");
                exportHeader.AppendLine(
                    "# Format: SoundType, PosX, PosY, PosZ, Radius, SoundIdDay, SoundIdNight, CooldownDay, CooldownNight, RandomDelay");
                Directory.CreateDirectory(exportPath);
                File.WriteAllText(exportPath + "sound_instances.txt", exportHeader.ToString() + soundExport);
            }

            if (musicExport.Length > 0)
            {
                StringBuilder exportHeader = new StringBuilder();
                exportHeader.AppendLine(LanternStrings.ExportHeaderTitle + "Music Instances");
                exportHeader.AppendLine(
                    "# Format: PosX, PosY, PosZ, Radius, MusicIndexDay, MusicIndexNight, DayLoopCount, NightLoopCount, FadeOutMs");
                Directory.CreateDirectory(exportPath);
                File.WriteAllText(exportPath + "music_instances.txt", exportHeader.ToString() + musicExport);
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