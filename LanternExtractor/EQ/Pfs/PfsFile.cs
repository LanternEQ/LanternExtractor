namespace LanternExtractor.EQ.Pfs
{
    /// <summary>
    /// This class represents a single file in the archive
    /// </summary>
    public class PfsFile
    {
        /// <summary>
        /// The CRC of the PFSFile
        /// </summary>
        public uint Crc { get; }

        /// <summary>
        /// The inflated size of the file in bytes
        /// </summary>
        public uint Size { get; }

        /// <summary>
        /// The positional offset of the file in the archive
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// The inflated bytes of the file
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// The name of the file
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes the constructor data
        /// </summary>
        /// <param name="crc">The CRC of the file</param>
        /// <param name="size">The size of the file in bytes</param>
        /// <param name="offset">The position of the file in the archive</param>
        /// <param name="bytes">The inflated bytes of the file</param>
        public PfsFile(uint crc, uint size, uint offset, byte[] bytes)
        {
            Crc = crc;
            Size = size;
            Offset = offset;
            Bytes = bytes;
        }

        /// <summary>
        /// Sets the file name - as this is calculated after the initial inflation
        /// </summary>
        /// <param name="fileName">The filename to set</param>
        public void SetFileName(string fileName)
        {
            Name = fileName;
        }
    }
}