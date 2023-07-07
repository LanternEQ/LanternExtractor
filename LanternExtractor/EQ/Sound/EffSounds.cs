using System;
using System.Collections.Generic;
using System.IO;
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
        public static int EntryLengthInBytes = 84;

        /// <summary>
        /// The sound bank referenced for sound names
        /// </summary>
        private readonly EffSndBnk _soundBank;
        private readonly EnvAudio _envAudio;
        private readonly string _soundFilePath;
        private readonly List<AudioInstance> _audioInstances = new List<AudioInstance>();

        public EffSounds(string soundFilePath, EffSndBnk soundBank, EnvAudio envAudio)
        {
            _soundFilePath = soundFilePath;
            _soundBank = soundBank;
            _envAudio = envAudio;
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
                var basePosition = EntryLengthInBytes * i;
                reader.BaseStream.Position = basePosition + 16;
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float posZ = reader.ReadSingle();
                float radius = reader.ReadSingle();

                reader.BaseStream.Position = basePosition + 56;

                var typeByte = reader.ReadByte();

                if (!Enum.IsDefined(typeof(AudioType), typeByte))
                {
                    logger.LogError($"Unable to parse sound type: {typeByte}");
                    continue;
                }

                var type = (AudioType)typeByte;
                reader.BaseStream.Position = basePosition + 48;
                int soundId1 = reader.ReadInt32();

                if (type == AudioType.Music)
                {
                    int soundId2 = reader.ReadInt32();
                    reader.BaseStream.Position = basePosition + 60;
                    int loopCountDay = reader.ReadInt32();
                    int loopCountNight = reader.ReadInt32();
                    int fadeOutMs = reader.ReadInt32();
                    var musicInstance = new MusicInstance(type, posX, posY, posZ, radius, soundId1, soundId2,
                        loopCountDay, loopCountNight, fadeOutMs);
                    _audioInstances.Add(musicInstance);
                }
                else if (type == AudioType.Sound2d)
                {
                    int soundId2 = reader.ReadInt32();
                    reader.BaseStream.Position = basePosition + 32;
                    int cooldown1 = reader.ReadInt32();
                    int cooldown2 = reader.ReadInt32();
                    reader.BaseStream.Position = basePosition + 60;
                    int volume = reader.ReadInt32();
                    var soundInstance = new SoundInstance2d(type, posX, posY, posZ, radius, volume, soundId1, cooldown1,
                        soundId2, cooldown2);
                    _audioInstances.Add(soundInstance);
                }
                else
                {
                    reader.BaseStream.Position = basePosition + 32;
                    int cooldown1 = reader.ReadInt32();
                    reader.BaseStream.Position = basePosition + 60;
                    int volume = reader.ReadInt32();
                    reader.BaseStream.Position = basePosition + 72;
                    int multiplier = reader.ReadInt32();
                    var soundInstance = new SoundInstance3d(type, posX, posY, posZ, radius, volume, soundId1, cooldown1,
                        multiplier);
                    _audioInstances.Add(soundInstance);
                }
            }
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

        private float GetEalSoundVolume(int soundId)
        {
            var soundName = GetSoundName(soundId);
            return _envAudio.GetVolumeLinear(soundName);
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

            foreach (var entry in _audioInstances)
            {
                if (entry.AudioType == AudioType.Music)
                {
                    var music = entry as MusicInstance;
                    if (music == null)
                    {
                        continue;
                    }

                    musicExport.AppendLine(string.Join(",", music.PosX, music.PosY, music.PosZ, music.Radius,
                        music.TrackIndexDay, music.TrackIndexNight, music.LoopCountDay, music.LoopCountNight,
                        music.FadeOutMs));
                }
                else if (entry.AudioType == AudioType.Sound2d)
                {
                    var sound2d = entry as SoundInstance2d;
                    if (sound2d == null)
                    {
                        continue;
                    }

                    soundExport.AppendLine(string.Join(",", (byte)sound2d.AudioType, sound2d.PosX, sound2d.PosY,
                        sound2d.PosZ, sound2d.Radius, GetSoundName(sound2d.SoundId1), GetSoundName(sound2d.SoundId2),
                        sound2d.Cooldown1, sound2d.Cooldown2, sound2d.Cooldown2, sound2d.Volume));
                }
                else
                {
                    var sound3d = entry as SoundInstance3d;
                    if (sound3d == null)
                    {
                        continue;
                    }

                    soundExport.AppendLine(string.Join(",", (byte)sound3d.AudioType, sound3d.PosX, sound3d.PosY,
                        sound3d.PosZ, sound3d.Radius, GetSoundName(sound3d.SoundId1), sound3d.Cooldown1, sound3d.Volume,
                        sound3d.Multiplier));
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
