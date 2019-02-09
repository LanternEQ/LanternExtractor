namespace LanternExtractor.EQ.Sound
{
    /// <summary>
    /// Describes the way the sound is heard by the player
    /// </summary>
    public enum SoundType : byte
    {
        /// <summary>
        /// Sounds that play at a constant volume
        /// </summary>
        NoFalloff = 0,

        /// <summary>
        /// Background music. Can specify both a daytime and nighttime music via the soundID
        /// Music usually has a large fade out delay
        /// </summary>
        Music = 1,

        /// <summary>
        /// Sounds that have a falloff - the further the player is from the center, the quieter it becomes
        /// </summary>
        Falloff = 2,

        /// <summary>
        /// Sounds that stay at full volume until the user has wandered outside of the FullVolRange
        /// </summary>
        FullVolumeRange = 3,
    }
}