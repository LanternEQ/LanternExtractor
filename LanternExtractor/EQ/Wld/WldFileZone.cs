﻿using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileZone : WldFile
    {
        public WldFileZone(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }

        protected override void ProcessData()
        {
            base.ProcessData();
            LinkBspReferences();

            if (_wldToInject != null)
            {
                ImportVertexColors();
            }

            if (_wldType == WldType.Objects)
            {
                FixSkeletalObjectCollision();
            }
        }

        private void FixSkeletalObjectCollision()
        {
            var actors = GetFragmentsOfType<Actor>();

            foreach (var actor in actors)
            {
                if (actor.ActorType != ActorType.Skeletal)
                {
                    continue;
                }

                var skeleton = actor.SkeletonReference.SkeletonHierarchy.Skeleton;

                foreach (var bone in skeleton)
                {
                    if (bone.Track.TrackDefFragment.Frames.Count != 1)
                    {
                        bone.MeshReference?.Mesh?.ClearCollision();
                    }
                }
            }
        }

        private void ImportVertexColors()
        {
            var colors = _wldToInject.GetFragmentsOfType<VertexColors>();

            if (colors.Count == 0)
            {
                return;
            }

            var meshes = GetFragmentsOfType<Mesh>();
            foreach (var vc in colors)
            {
                var name = vc.Name.Split('_')[0] + "_DMSPRITEDEF";

                var fragment = GetFragmentByName<Mesh>(name);

                if (fragment != null)
                {
                    fragment.Colors = vc.Colors;
                }
            }
        }

        private void LinkBspReferences()
        {
            var bspTree = GetFragmentsOfType<BspTree>();
            var bspRegions = GetFragmentsOfType<BspRegion>();
            var regionTypes = GetFragmentsOfType<BspRegionType>();


            if (bspTree.Count == 0 || bspRegions.Count == 0 || regionTypes.Count == 0)
            {
                return;
            }

            bspTree[0].LinkBspRegions(bspRegions);
            
            foreach (var regionType in regionTypes)
            {
                regionType.LinkRegionType(bspRegions);
            }
        }
        
        protected override void ExportData()
        {
            base.ExportData();
            ExportAmbientLightColor();
            ExportBspTree();
        }

        private void ExportAmbientLightColor()
        {
            var ambientLight = GetFragmentsOfType<GlobalAmbientLight>();
            
            if (ambientLight.Count == 0)
            {
                return;
            }
            
            AmbientLightColorWriter writer = new AmbientLightColorWriter();
            writer.AddFragmentData(ambientLight[0]);
            writer.WriteAssetToFile(GetExportFolderForWldType() + "/ambient_light.txt");        
        }

        private void ExportBspTree()
        {
            var bspTree = GetFragmentsOfType<BspTree>();
            
            if (bspTree.Count == 0)
            {
                return;
            }
            
            BspTreeWriter writer = new BspTreeWriter();
            writer.AddFragmentData(bspTree[0]);
            writer.WriteAssetToFile(GetExportFolderForWldType() + "/bsp_tree.txt");
        }
    }
}