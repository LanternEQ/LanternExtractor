using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Helpers
{
    public static class FragmentNameCleaner
    {
        public static string CleanName(WldFragment fragment)
        {
            switch(fragment.Type)
            {
                case FragmentType.MaterialList:
                    return fragment.Name.Replace("_MP", "").ToLower();
                case FragmentType.Material:
                    return fragment.Name.Replace("_MDF", "").ToLower();
                case FragmentType.Mesh:
                    return fragment.Name.Replace("_DMSPRITEDEF", "").ToLower();
                case FragmentType.ModelReference:
                    return fragment.Name.Replace("_ACTORDEF", "").ToLower();
                case FragmentType.SkeletonHierarchy:
                    return fragment.Name.Replace("_HS_DEF", "").ToLower();
                case FragmentType.TrackDefFragment:
                    return fragment.Name.Replace("_TRACKDEF", "").ToLower();
                case FragmentType.TrackFragment:
                    return fragment.Name.Replace("_TRACK", "").ToLower();
            }

            return "";
        }
    }
}