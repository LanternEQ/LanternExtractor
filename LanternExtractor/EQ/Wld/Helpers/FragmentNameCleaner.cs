using System;
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
                    break;
                case FragmentType.Mesh:
                    return fragment.Name.Replace("_DMSPRITEDEF", "").ToLower();
                case FragmentType.MeshReference:
                    break;
            }

            return "";
        }
    }
}