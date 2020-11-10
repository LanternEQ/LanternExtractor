using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Camera Reference (0x09)
    /// References a Camera fragment
    /// </summary>
    class CameraReference : WldFragment
    {
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
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("CameraReference: Reference: " + (Camera.Index + 1));
        }
    }
}