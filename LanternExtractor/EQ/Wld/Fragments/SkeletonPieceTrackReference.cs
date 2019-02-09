using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x13 - Skeleton Piece Track Reference
    /// Refers to the skeleton piece fragment (0x12)
    /// </summary>
    public class SkeletonPieceTrackReference : WldFragment
    {
        /// <summary>
        /// Reference to a skeleton piece
        /// </summary>
        public SkeletonPiece SkeletonPiece { get; set; }

        public bool Assigned { get; set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            SkeletonPiece = fragments[reference - 1] as SkeletonPiece;
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (SkeletonPiece != null)
            {
                logger.LogInfo("-----");
                logger.LogInfo("0x13: Skeleton piece reference: " + SkeletonPiece.Index + 1);
            }
        }
    }
}