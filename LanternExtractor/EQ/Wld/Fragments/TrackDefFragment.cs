using System;
using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x12 - Skeleton Piece
    /// Describes how a part of a skeleton is rotated and shifted in relation to the parent
    /// </summary>
    public class TrackDefFragment : WldFragment
    {
        /// <summary>
        /// A list of bone positions for each frame
        /// </summary>
        public List<BonePosition> Frames { get; private set; }
        public List<BoneTransform> Frames2 { get; private set; }
        
        public bool IsAssigned;

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            // Flags are always 8 when dealing with object animations
            if (flags != 8)
            {
                
            }

            BitAnalyzer bitAnalyzer = new BitAnalyzer(flags);

            bool hasData2Values = bitAnalyzer.IsBitSet(3);
            
            int frameCount = reader.ReadInt32();
            
            if (Name.ToLower().Contains("it153") && Name.ToLower().Contains("c05") && frameCount != 1)
            {
                
            }

            Frames = new List<BonePosition>();
            Frames2 = new List<BoneTransform>();

            float l = 0.0f;

            for (int i = 0; i < frameCount; ++i)
            {
                // Windcatcher
                Int16 rotDenominator = reader.ReadInt16();
                Int16 rotX = reader.ReadInt16();
                Int16 rotY = reader.ReadInt16();
                Int16 rotZ = reader.ReadInt16();
                Int16 shiftX = reader.ReadInt16();
                Int16 shiftY = reader.ReadInt16();
                Int16 shiftZ = reader.ReadInt16();
                Int16 shiftDenominator = reader.ReadInt16();
                
                BoneTransform frameTransform = new BoneTransform();
                
                if (shiftDenominator != 0)
                {
                    string partName = Name;
                    double scale = 1.0f / shiftDenominator;
                    double x = shiftX / 256f;
                    double y = shiftY / 256f;
                    double z = shiftZ / 256f;

                    frameTransform.Scale = shiftDenominator / 256f;

                    frameTransform.Translation = new vec3((float)x, (float)y, (float)z);
                }
                else
                {
                    frameTransform.Translation = vec3.Zero;
                }

                
                frameTransform.Rotation = new quat(rotX, rotY, rotZ, rotDenominator).Normalized;

                Frames2.Add(frameTransform);
            }

            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
        }
        private float RadianToDegree(float angle)
        {
            return angle * (180.0f / (float)Math.PI);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x12: Bone frame count: " + Frames.Count);
        }
    }
}