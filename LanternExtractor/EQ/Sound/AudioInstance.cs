namespace LanternExtractor.EQ.Sound
{
    public abstract class AudioInstance
    {
        protected AudioInstance(AudioType type, float posX, float posY, float posZ, float radius)
        {
            AudioType = type;
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
            Radius = radius;
        }

        public AudioType AudioType { get; }
        public float PosX { get; }
        public float PosY { get; }
        public float PosZ { get; }
        public float Radius { get; }
    }

    public class MusicInstance : AudioInstance
    {
        public MusicInstance(AudioType type, float posX, float posY, float posZ, float radius, int trackIndexDay,
            int trackIndexNight, int loopCountDay, int loopCountNight, int fadeOutMs) : base(type, posX, posY, posZ,
            radius)
        {
            TrackIndexDay = trackIndexDay;
            TrackIndexNight = trackIndexNight;
            LoopCountDay = loopCountDay;
            LoopCountNight = loopCountNight;
            FadeOutMs = fadeOutMs;
        }

        public int TrackIndexDay { get; }
        public int TrackIndexNight { get; }
        public int LoopCountDay { get; }
        public int LoopCountNight { get; }
        public int FadeOutMs { get; }
    }

    public abstract class SoundInstance : AudioInstance
    {
        public string Sound1 { get; }
        public float Volume1 { get; }
        public int Cooldown1 { get; }
        public int CooldownRandom { get; }

        protected SoundInstance(AudioType type, float posX, float posY, float posZ, float radius, float volume1,
            string sound1, int cooldown1, int cooldownRandom) : base(type, posX, posY, posZ, radius)
        {
            Sound1 = sound1;
            Volume1 = volume1;
            Cooldown1 = cooldown1;
            CooldownRandom = cooldownRandom;
        }
    }

    public class SoundInstance2d : SoundInstance
    {
        public string Sound2 { get; }
        public float Volume2 { get; }
        public int Cooldown2 { get; }

        public SoundInstance2d(AudioType type, float posX, float posY, float posZ, float radius, float volume1,
            string sound1, int cooldown1, string sound2, int cooldown2,  int cooldownRandom, float volume2) : base(type, posX, posY, posZ, radius, volume1,
            sound1, cooldown1, cooldownRandom)
        {
            Sound2 = sound2;
            Cooldown2 = cooldown2;
            Volume2 = volume2;
        }
    }

    public class SoundInstance3d : SoundInstance
    {
        public int Multiplier;

        public SoundInstance3d(AudioType type, float posX, float posY, float posZ, float radius, float volume,
            string sound1, int cooldown1, int cooldownRandom, int multiplier) : base(type, posX, posY, posZ, radius, volume, sound1,
            cooldown1, cooldownRandom)
        {
            Multiplier = multiplier;
        }
    }
}