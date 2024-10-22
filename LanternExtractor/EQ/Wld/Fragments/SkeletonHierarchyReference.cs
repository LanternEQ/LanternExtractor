using System.Collections.Generic;
using System.IO;
using Serilog;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// SkeletonHierarchyReference (0x11)
    /// Internal name: None
    /// A reference to a skeleton track fragment (0x12)
    /// </summary>
    class SkeletonHierarchyReference : WldFragment
    {
        public SkeletonHierarchy SkeletonHierarchy { get; set; }

        public override void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat);

            var reader = new BinaryReader(new MemoryStream(data));

            // Reference is usually 0
            // Confirmed
            Name = stringHash[-reader.ReadInt32()];

            int reference = reader.ReadInt32();

            SkeletonHierarchy = fragments[reference - 1] as SkeletonHierarchy;

            if (SkeletonHierarchy == null)
            {
                Log.Error("Bad skeleton hierarchy reference");
            }

            int params1 = reader.ReadInt32();

            // Params are 0
            // Confirmed
            if (params1 != 0)
            {

            }

            // Confirmed end
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {

            }
        }

        public override void OutputInfo()
        {
            base.OutputInfo();

            if (SkeletonHierarchy != null)
            {
                Log.Information("-----");
                Log.Information("0x11: Skeleton track reference: " + SkeletonHierarchy.Index + 1);
            }
        }
    }
}
