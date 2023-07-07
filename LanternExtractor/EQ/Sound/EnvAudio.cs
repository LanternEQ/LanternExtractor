using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EalTools;

namespace LanternExtractor.EQ.Sound
{
    public class EnvAudio
    {
        public static EnvAudio Instance => _instance;
        private static readonly EnvAudio _instance = new EnvAudio();

        public EalData Data { get; private set; }

        private string _ealFilePath;
        private EalFile _ealFile;
        private Dictionary<string, int> _sourceLevels;

        static EnvAudio()
        {
        }

        private EnvAudio()
        {
        }

        public bool Load(string ealFilePath)
        {
            if (_ealFilePath == ealFilePath)
            {
                return false;
            }

            _ealFilePath = ealFilePath;

            if (_ealFilePath == null || !File.Exists(_ealFilePath))
            {
                return false;
            }

            _ealFile = new EalFile(_ealFilePath);

            if (!_ealFile.Initialize())
            {
                return false;
            }

            Data = _ealFile.Data;

            if (Data == null)
            {
                return false;
            }

            _sourceLevels = Data.SourceModels.ToDictionary(
                s => Path.GetFileNameWithoutExtension(s.Name).ToLower(),
                s => s.SourceAttributes.EaxAttributes.DirectPathLevel
            );

            return true;
        }

        public int GetVolumeEq(string soundFile)
        {
            var volume = 0;
            _sourceLevels?.TryGetValue(soundFile, out volume);
            return volume;
        }

        public float GetVolumeLinear(string soundFile)
        {
            var volumeEq = GetVolumeEq(soundFile);
            var linear = MathF.Pow(10.0f, volumeEq / 2000.0f);
            return Math.Clamp(linear, 0f, 1f);
        }
    }
}
