using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld;
using LanternExtractor.Infrastructure.Logger;
using System.Collections.Generic;

namespace LanternExtractor.EQ
{
    public sealed class GlobalReference
    {
        // Note: NOT thread-safe, however, outside of init, should be used
        // read-only. If app is running multi-threaded the init happens
        // before tasks are spun up.
        public static WldFileCharacters CharacterWld { get; private set; }
        public static PfsArchive CharacterWldPfsArchive { get; private set; }

        public static void InitCharacterWld( PfsArchive pfsArchive, PfsFile wldFile, string rootFolder, string zoneName, 
            WldType type, ILogger logger, Settings settings, List<WldFile> wldFilesToInject = null )
        {
            CharacterWld = new WldFileCharacters(wldFile, zoneName, type, logger, settings, wldFilesToInject);
            CharacterWld.Initialize(rootFolder, false);
            CharacterWldPfsArchive = pfsArchive;
        }
    }
}
