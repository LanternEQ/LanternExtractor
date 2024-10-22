namespace LanternExtractor.EQ.Archive
{
    public class NullArchive : ArchiveBase
    {
        public NullArchive(string filePath) : base(filePath)
        {
        }

        public override bool Initialize()
        {
            return false;
        }
    }
}
