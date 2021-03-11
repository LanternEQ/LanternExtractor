using System.Collections.Generic;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Fragment17 (0x18) - PolygonAnimation?
    /// Internal Name: _POLYHDEF
    /// Need to figure this fragment out.
    /// </summary>
    public class Fragment17 : WldFragment
    {
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            Name = stringHash[-Reader.ReadInt32()];
            int flags = Reader.ReadInt32();
            int size1 = Reader.ReadInt32();
            int size2 = Reader.ReadInt32();
            float unknown = Reader.ReadSingle();

            for (int i = 0; i < size1; ++i)
            {
                float x = Reader.ReadSingle();
                float y = Reader.ReadSingle();
                float z = Reader.ReadSingle();
            }
        }
    }
}