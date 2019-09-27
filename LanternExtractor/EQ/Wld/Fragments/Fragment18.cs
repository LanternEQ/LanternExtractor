using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    class Fragment18 : WldFragment
    {
        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];
            
        }
        
        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
        }
    }
}