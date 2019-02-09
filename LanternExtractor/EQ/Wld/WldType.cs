namespace LanternExtractor.EQ.Wld
{
    /// <summary>
    /// The type of WLD file that is being loaded
    /// </summary>
    public enum WldType
    {
        /// <summary>
        /// The main zone WLD containing zone geometry
        /// arena.s3d, arena.wld
        /// </summary>
        Zone,

        /// <summary>
        /// The WLD that contains the positional information about the zone objects
        /// arena.s3d, objects.wld
        /// </summary>
        ZoneObjects,

        /// <summary>
        /// The WLD containing all light information
        /// arena.s3d, lights.wld
        /// </summary>
        Lights,

        /// <summary>
        /// The WLD containing model geometry
        /// arena_obj.s3d, arena_obj.wld
        /// </summary>
        Objects,

        /// <summary>
        /// The WLD containing information about the sky (only found in sky.s3d)
        /// sky.s3d, sky.wld
        /// </summary>
        Sky,

        /// <summary>
        /// The WLD containing the character model and information
        /// </summary>
        Characters
    }
}