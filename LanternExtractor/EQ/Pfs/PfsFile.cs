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
        
        public PfsFile(uint crc, uint size, uint offset, byte[] bytes)
        {
            Crc = crc;
            Size = size;
            Offset = offset;
            Bytes = bytes;
        }
    }
}