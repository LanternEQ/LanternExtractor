using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Helpers
{
    public static class FragmentNameCleaner
    {
        public static string CleanName(WldFragment fragment, bool toLower = true)
        {
            string cleanedName = string.Empty;
            
            switch(fragment.Type)
            {
                case FragmentType.MaterialList:
                    cleanedName = fragment.Name.Replace("_MP", "");
                    break;
                case FragmentType.Material:
                    cleanedName = fragment.Name.Replace("_MDF", "");
                    break;
                case FragmentType.Mesh:
                    cleanedName = fragment.Name.Replace("_DMSPRITEDEF", "");                    
                    break;
                case FragmentType.ModelReference:
                    cleanedName = fragment.Name.Replace("_ACTORDEF", "");
                    break;
                case FragmentType.SkeletonHierarchy:
                    cleanedName = fragment.Name.Replace("_HS_DEF", "");
                    break;
                case FragmentType.TrackDefFragment:
                    cleanedName = fragment.Name.Replace("_TRACKDEF", "");
                    break;
                case FragmentType.TrackFragment:
                    cleanedName = fragment.Name.Replace("_TRACK", "");
                    break;
            }

            if(toLower)
            {
                cleanedName = cleanedName.ToLower();
            }
            
            return cleanedName;
        }
    }
}