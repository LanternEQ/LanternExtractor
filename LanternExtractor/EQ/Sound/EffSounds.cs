using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LanternExtractor.Infrastructure.Logger;

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

        public void Initialize(ILogger logger)
        {
            if (_soundBank == null || !File.Exists(_soundFilePath))
            {
                return;
            }

            var file = File.Open(_soundFilePath, FileMode.Open);
            var reader = new BinaryReader(file);
            int fileLength = (int)reader.BaseStream.Length;

            if (fileLength % EntryLengthInBytes != 0)
            {
                logger.LogError($"Incorrect .eff file - size must be multiple of {EntryLengthInBytes}");
                return;
            }

            int entryCount = fileLength / EntryLengthInBytes;

            for (int i = 0; i < entryCount; ++i)
            {
                var newSound = new SoundEntry
                {
                    UnkRef00 = reader.ReadInt32(),
                    UnkRef04 = reader.ReadInt32(),
                    Reserved = reader.ReadInt32(),
                    Sequence = reader.ReadInt32(),
                    PosX = reader.ReadSingle(),
                    PosY = reader.ReadSingle(),
                    PosZ = reader.ReadSingle(),
                    Radius = reader.ReadSingle(),
                    CooldownDay = reader.ReadInt32(),
                    CooldownNight = reader.ReadInt32(),
                    RandomDelay = reader.ReadInt32(),
                    Unk44 = reader.ReadInt32(),
                    SoundIdDay = reader.ReadInt32(),
                    SoundIdNight = reader.ReadInt32(),
                    SoundType = (SoundType)reader.ReadByte(),
                    UnkPad57 = reader.ReadByte(),
                    UnkPad58 = reader.ReadByte(),
                    UnkPad59 = reader.ReadByte(),
                    AsDistance = reader.ReadInt32(),
                    UnkRange64 = reader.ReadInt32(),
                    FadeOutMs = reader.ReadInt32(),
                    UnkRange72 = reader.ReadInt32(),
                    FullVolRange = reader.ReadInt32(),
                    UnkRange80 = reader.ReadInt32()
                };
                
                _soundEntries.Add(newSound);

                if (newSound.SoundType == SoundType.Music && newSound.FullVolRange != 1000 && newSound.FullVolRange != 0)
                {
                    
                }
            }
            
            var writer = new BinaryWriter(file);
            file.Position = 0;
            ModifyMusicData(writer);
        }

        private void ModifyMusicData(BinaryWriter writer)
        {
            // MUSIC EXPLORATION
            /*writer.BaseStream.Position = 16;
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(0);
            writer.BaseStream.Position = 48;
            writer.Write(0);
            writer.Write(4);
            writer.BaseStream.Position = 60; 
            writer.Write(3);
            writer.BaseStream.Position = 68;
            writer.Write(1000);
            writer.Write(1000);
            writer.BaseStream.Position = 76;
            writer.Write(500);
            writer.BaseStream.Position = 0;
            var memoryStream = new MemoryStream();
            writer.BaseStream.CopyTo(memoryStream);
            File.WriteAllBytes("arena_sounds.eff", memoryStream.ToArray());*/
            
            // SOUND EXPLORATION
            /*writer.BaseStream.Position = 16;
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(0f);
            writer.BaseStream.Position = 32;
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.BaseStream.Position = 76;
            writer.Write(100);
            writer.BaseStream.Position = 0;
            var memoryStream = new MemoryStream();
            writer.BaseStream.CopyTo(memoryStream);
            writer.BaseStream.CopyTo(memoryStream);
            writer.BaseStream.CopyTo(memoryStream);
            File.WriteAllBytes("arena_sounds.eff", memoryStream.ToArray());*/
            
            // Sound isolation
            /*var memoryStream = new MemoryStream();
            writer.BaseStream.CopyTo(memoryStream);
            File.WriteAllBytes("arena_sounds.eff", memoryStream.ToArray().Skip(14 * EntryLengthInBytes).Take(EntryLengthInBytes).ToArray());*/
        }
        
        private string GetSoundName(int soundId)
        {
            var emissionType = GetEmissionType(soundId);
            switch (emissionType)
            {
                case EmissionType.None:
                    return string.Empty;
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
                    soundExport.Append(GetSoundName(entry.SoundIdDay));
                    soundExport.Append(",");
                    soundExport.Append(GetSoundName(entry.SoundIdNight));
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
                    "# Format: PosX, PosY, PosZ, Radius, MusicIndexDay, MusicIndexNight, LoopCountDay, LoopCountNight, FadeOutMs");
                Directory.CreateDirectory(exportPath);
                File.WriteAllText(exportPath + "music_instances.txt", exportHeader.ToString() + musicExport);
            }
        }
    }
}