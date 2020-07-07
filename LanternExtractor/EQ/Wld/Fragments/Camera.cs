using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Camera (0x08)
    /// A fragment that is not understood. Contains 26 parameters. It's here in case someone wants to take a look.
    /// </summary>
    class Camera : WldFragment
    {
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            long cachedPosition = reader.BaseStream.Position;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int value = reader.ReadInt32();
            }

            reader.BaseStream.Position = cachedPosition;
            
            Name = stringHash[-reader.ReadInt32()];

            // 26 fields - unknown what they reference
            int params0 = reader.ReadInt32();
            int params1 = reader.ReadInt32();
            int params2 = reader.ReadInt32();
            int params3 = reader.ReadInt32();
            int params4 = reader.ReadInt32();
            float params5 = reader.ReadSingle();
            float params6 = reader.ReadSingle();
            int params7 = reader.ReadInt32();
            float params8 = reader.ReadSingle();
            float params9 = reader.ReadSingle();
            int params10 = reader.ReadInt32();
            float params11 = reader.ReadSingle();
            float params12 = reader.ReadSingle();
            int params13 = reader.ReadInt32();
            float params14 = reader.ReadSingle();
            float params15 = reader.ReadSingle();
            int params16 = reader.ReadInt32();
            int params17 = reader.ReadInt32();
            int params18 = reader.ReadInt32();
            int params19 = reader.ReadInt32();
            int params20 = reader.ReadInt32();
            int params21 = reader.ReadInt32();
            int params22 = reader.ReadInt32();
            int params23 = reader.ReadInt32();
            int params24 = reader.ReadInt32();
            int params25 = reader.ReadInt32();

            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
        }
    }
}