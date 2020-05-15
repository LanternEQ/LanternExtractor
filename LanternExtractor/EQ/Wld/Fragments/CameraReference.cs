using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x09 - Camera Reference
    /// References a camera fragment (0x08)
    /// </summary>
    class CameraReference : WldFragment
    {
        /// <summary>
        /// Reference to a camera fragment (0x08)
        /// </summary>
        public Camera Camera { get; private set; }

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            Camera = fragments[reader.ReadInt32() - 1] as Camera;

            // Usually 0
            int flags = reader.ReadInt32();

            if (flags != 0)
            {
                
            }

            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x09: Reference: " + (Camera.Index + 1));
        }
    }
}