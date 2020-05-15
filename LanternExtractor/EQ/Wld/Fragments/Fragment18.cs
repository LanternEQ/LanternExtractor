using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public class Fragment18 : WldFragment
    {
        public Fragment17 _fragment17Reference;
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int fragment17Reference = reader.ReadInt32();

            _fragment17Reference = fragments[fragment17Reference - 1] as Fragment17;
            
            float params1 = reader.ReadSingle();

            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
        }
        
        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
        }
    }
}