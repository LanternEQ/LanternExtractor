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
        public int Volume { get; }
        public int SoundId1 { get; }
        public int Cooldown1 { get; }

        protected SoundInstance(AudioType type, float posX, float posY, float posZ, float radius, int volume,
            int soundId1, int cooldown1) : base(type, posX, posY, posZ, radius)
        {
            Volume = volume;
            SoundId1 = soundId1;
            Cooldown1 = cooldown1;
        }
    }

    public class SoundInstance2d : SoundInstance
    {
        public int SoundId2 { get; }
        public int Cooldown2 { get; }

        public SoundInstance2d(AudioType type, float posX, float posY, float posZ, float radius, int volume,
            int soundId1, int cooldown1, int soundId2, int cooldown2) : base(type, posX, posY, posZ, radius, volume,
            soundId1, cooldown1)
        {
            SoundId2 = soundId2;
            Cooldown2 = cooldown2;
        }
    }

    public class SoundInstance3d : SoundInstance
    {
        public int Multiplier;

        public SoundInstance3d(AudioType type, float posX, float posY, float posZ, float radius, int volume,
            int soundId1, int cooldown1, int multiplier) : base(type, posX, posY, posZ, radius, volume, soundId1,
            cooldown1)
        {
            Multiplier = multiplier;
        }
    }
}