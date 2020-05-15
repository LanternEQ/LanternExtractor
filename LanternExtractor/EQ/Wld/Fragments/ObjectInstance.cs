using System.Collections.Generic;
using System.IO;
using GlmSharp;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Object Instance (0x15)
    /// Information about a single instance of an actor spawn.
    /// </summary>
    class ObjectInstance : WldFragment
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

        public VertexColors Colors;

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            // Always 0 DW
            int unknown = reader.ReadInt32();

            if (unknown != 0)
            {
                
            }
            
            // in main zone, points to 0x16, in object wld, it contains the object name
            int reference = reader.ReadInt32();

            if (reference < 0)
            {
                ObjectName = stringHash[-reference].Replace("_ACTORDEF", "");
            }
            else
            {
                ObjectName = string.Empty;
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

            // Fragment reference
            // In main zone, it points to a 0x16 fragment
            // In objects.wld, it is 0
            int unknown2 = reader.ReadInt32();

            if (unknown2 != 0)
            {
                
            }
            
            // TODO: Are these safe coords in the main zone file? they come from server
            Position = new vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            if (ObjectName.Contains("TEMP"))
            {
                
            }
            
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

            if (scaleX != 0f)
            {
                
            }
            
            Scale = new vec3(scaleY, scaleY, scaleY);
            
            int colorFragment = reader.ReadInt32();

            if (colorFragment != 0)
            {
                Colors = (fragments[colorFragment - 1 ] as VertexColorReference)?.VertexColors;
                
                int something = reader.ReadInt32();

                if (something != 0)
                {
                    
                }
            }
            
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
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