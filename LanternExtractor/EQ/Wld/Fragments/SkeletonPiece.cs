using System;
using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x12 - Skeleton Piece
    /// Describes how a part of a skeleton is rotated and shifted in relation to the parent
    /// </summary>
    public class SkeletonPiece : WldFragment
    {
        /// <summary>
        /// A list of bone positions for each frame
        /// </summary>
        public List<BonePosition> Frames { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            int size1 = reader.ReadInt32();

            Frames = new List<BonePosition>();

            for (int i = 0; i < size1; ++i)
            {
                BonePosition bonePosition = new BonePosition();

                // If this is 0, use the parent rotation
                short rotateDenominator = reader.ReadInt16();
                short rotateXNumerator = reader.ReadInt16();
                short rotateYNumerator = reader.ReadInt16();
                short rotateZNumerator = reader.ReadInt16();

                if (rotateDenominator != 0)
                {
                    // The first rotation method - from the WLD doc
                    var rot = new vec3((float) rotateXNumerator / rotateDenominator,
                        (float) rotateYNumerator / rotateDenominator,
                        (float) rotateZNumerator / rotateDenominator);

                    bonePosition.Rotation = new quat(rot).Normalized;

                    // The second method - from EQuilibre Client and Peter
                    float l = (float) Math.Sqrt((float) (rotateDenominator * rotateDenominator
                                                         + rotateXNumerator * rotateXNumerator +
                                                         rotateYNumerator * rotateYNumerator
                                                         + rotateZNumerator * rotateZNumerator));
                }
                else
                {
                    // The second method - from EQuilibre Client and Peter
                    float l = (float) Math.Sqrt((float) (rotateDenominator * rotateDenominator
                                                         + rotateXNumerator * rotateXNumerator +
                                                         rotateYNumerator * rotateYNumerator
                                                         + rotateZNumerator * rotateZNumerator));
                }

                short shiftXNumerator = reader.ReadInt16();
                short shiftYNumerator = reader.ReadInt16();
                short shiftZNumerator = reader.ReadInt16();
                short shiftDenominator = reader.ReadInt16();

                if (shiftDenominator != 0)
                {
                    float scale = 1.0f / shiftDenominator;
                    bonePosition.Translation = new vec3((float) shiftXNumerator * scale,
                        (float) shiftYNumerator * scale,
                        (float) shiftZNumerator * scale);
                }

                Frames.Add(bonePosition);
            }

            // 4 dwords - unknown
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x12: Bone frame count: " + Frames.Count);
        }
    }
}