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
    public class TrackDefFragment : WldFragment
    {
        /// <summary>
        /// A list of bone positions for each frame
        /// </summary>
        public List<BonePosition> Frames { get; private set; }
        public List<BoneTransform> Frames2 { get; private set; }
        
        public bool IsAssigned;

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
            Frames2 = new List<BoneTransform>();

            float l = 0.0f;

            for (int i = 0; i < size1; ++i)
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

                if (rotDenominator != 0)
                {
                    l = (float)Math.Sqrt((float)(rotDenominator * rotDenominator + rotX * rotX + rotY * rotY + rotZ * rotZ));
                    
                    frameTransform.Rotation2 = new vec4(rotX / l, rotY / l, rotZ / l, rotDenominator / l);
                    frameTransform.Rotation3 = new vec3(RadianToDegree((float)rotX / rotDenominator), RadianToDegree((float)rotY / rotDenominator),
                        RadianToDegree((float)rotZ / rotDenominator));
                }
                
                if (shiftDenominator != 0)
                {
                    string partName = Name;
                    double scale = 1.0f / shiftDenominator;
                    double x = shiftX * scale;
                    double y = shiftY * scale;
                    double z = shiftZ * scale;
                    frameTransform.Translation = new vec3((float)x, (float)y, (float)z);

                    if (Name.Contains("GOR") && i == 0)
                    {
                        
                    }
                }
                else
                {
                    frameTransform.Translation = vec3.Zero;
                }
                
                frameTransform.Rotation = new quat(rotX, rotY, rotZ, rotDenominator).Normalized;
                
                if (index == 3559)
                {
                    //logger.LogError($"{i}: NOT NORM {frameTransform.Rotation}");
                    //logger.LogError($"{i}: NORM {frameTransform.Rotation.Normalized}");
                }
                
                Frames2.Add(frameTransform);
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