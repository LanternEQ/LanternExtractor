using System.Collections.Generic;
using System.IO;
using Serilog;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// A common base abstract class containing the common properties of all WLD fragments
    /// </summary>
    public abstract class WldFragment
    {
        public int Index { get; private set; }

        /// <summary>
        /// The size of the fragment in bytes
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// The name of the fragment - not always used
        /// </summary>
        public string Name { get; set; }

        public BinaryReader Reader { get; set; }

        /// <summary>
        /// Initializes the WLD fragment and handles it based on the type
        /// </summary>
        /// <param name="index"></param>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="data">The bytes that make up the fragments</param>
        /// <param name="fragments">A dictionary of all other fragments for referencing</param>
        /// <param name="stringHash">The string hash - for fragment name assignment</param>
        /// <param name="isNewWldFormat"></param>
        public virtual void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat)
        {
            Index = index;
            Size = size;
            Reader = new BinaryReader(new MemoryStream(data));
        }

        /// <summary>
        /// Outputs general information about the fragment
        /// </summary>
        public virtual void OutputInfo()
        {
            Log.Information("-----------------------------------");
            Log.Information("Fragment " + (Index + 1) + ": " + this.GetType().Name);
            Log.Information("-----");
            Log.Information("Size: " + Size + " bytes");
            Log.Information("Name: " + (string.IsNullOrEmpty(Name) ? "(empty)" : Name));
        }
    }
}
