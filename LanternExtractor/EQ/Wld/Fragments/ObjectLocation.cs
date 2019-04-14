using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x15 - Object Location
    /// Contains information about a single zone object instance
    /// </summary>
    class ObjectLocation : WldFragment
    {
        /// <summary>
        /// The name of the object model
        /// </summary>
        public string ObjectName { get; private set; }

        /// <summary>
        /// The instance position in the world
        /// </summary>
        public vec3 Position { get; private set; }

        /// <summary>
        /// The instance rotation in the world
        /// </summary>
        public vec3 Rotation { get; private set; }

        /// <summary>
        /// The instance scale in the world
        /// </summary>
        public vec3 Scale { get; private set; }

        //public VertexColors VertexColor;

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            // No name reference
            int unknown = reader.ReadInt32();

            // in main zone, points to 0x14, in object wld, it contains the object name
            int reference = reader.ReadInt32();

            if (reference < 0)
            {
                ObjectName = stringHash[-reference].Split('_')[0].ToLower();
            }

            int flags = reader.ReadInt32();

            if (flags == 0x2E)
            {
                // main zone wld
            }
            else if (flags == 0x32E)
            {
                // object wld
            }

            int unknown2 = reader.ReadInt32();

            Position = new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            // Rotation is strange. There is never any x rotation (roll)
            // The z rotation is negated
            float value0 = reader.ReadSingle();
            float value1 = reader.ReadSingle();
            float value2 = reader.ReadSingle();

            float modifier = 1.0f / 512.0f * 360.0f;
            
            Rotation = new vec3(0f, ( value1 * modifier ), -(value0 * modifier));      

            // Only scale y is used
            float scaleX, scaleY, scaleZ;
            scaleX = reader.ReadSingle();    
            scaleY = reader.ReadSingle();
            scaleZ = reader.ReadSingle();
            
            Scale = new vec3(scaleY, scaleY, scaleY);

            // Vertex colors are not used
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x15: Name: " + ObjectName);
            logger.LogInfo("0x15: Position: " + Position);
            logger.LogInfo("0x15: Rotation: " + Rotation);
            logger.LogInfo("0x15: Scale: " + Scale);
        }
    }
}