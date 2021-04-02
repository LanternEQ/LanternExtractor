using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileEquipment : WldFile
    {
        public WldFileEquipment(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }
        
        protected override void ExportData()
        {
            DoAllSkeletons();
            FindUnhandledSkeletons();
            FindAdditionalAnimations();
            base.ExportData();
            ExportParticleSystems();
            ExportSkeletonsNew();
        }

        private void DoAllSkeletons()
        {
            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();

            foreach (var skeleton in skeletons)
            {
                skeleton.BuildSkeletonData(false);
            }
        }

        private void ExportParticleSystems()
        {
            var particles = GetFragmentsOfType<ParticleCloud>();

            foreach (var particle in particles)
            {
                var writer = new ParticleSystemWriter();
                writer.AddFragmentData(particle);
                writer.WriteAssetToFile(GetRootExportFolder() + "/Particles/" + FragmentNameCleaner.CleanName(particle) + ".txt");
            }
        }
        
        private void ExportSkeletonsNew()
        {
            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();

            foreach (var skeleton in skeletons)
            {
                var writer = new SkeletonHierarchyNewWriter(false);
                writer.AddFragmentData(skeleton);
                writer.WriteAssetToFile(GetRootExportFolder() + "/SkeletonsNew/" + FragmentNameCleaner.CleanName(skeleton) + ".txt");
            }
        }

        private void FindUnhandledSkeletons()
        {
            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();
            if (skeletons == null)
            {
                return;
            }

            foreach (WldFragment fragment in skeletons)
            {
                SkeletonHierarchy skeleton = (SkeletonHierarchy)fragment;

                if (skeleton == null)
                {
                    continue;
                }

                if (skeleton.IsAssigned)
                {
                    continue;
                }

                string cleanedName = FragmentNameCleaner.CleanName(skeleton, false);
                string actorName = cleanedName + "_ACTORDEF";
                
                if (!_fragmentNameDictionary.ContainsKey(actorName))
                {
                    continue;
                }

                (_fragmentNameDictionary[actorName] as Actor)?.AssignSkeletonReference(skeleton, _logger);
            }
        }

        private void FindAdditionalAnimations()
        {
            var animations = GetFragmentsOfType<TrackFragment>();
            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();

            foreach (var track in animations)
            {
                if (track == null)
                {
                    continue;
                }

                if (track.IsPoseAnimation)
                {
                    continue;
                }

                if (track.IsProcessed)
                {
                    continue;
                }

                
                
                foreach (var skeleton in skeletons)
                {
                    string boneName = string.Empty;
                    if (skeleton.IsValidSkeleton(FragmentNameCleaner.CleanName(track), out boneName))
                    {
                        _logger.LogError($"Assigning {track.Name} to {skeleton.Name}");
                        track.IsProcessed = true;
                        skeleton.AddTrackDataEquipment(track, boneName.ToLower());
                    }
                }

                if (!track.IsNameParsed)
                {
                    //track.ParseTrackDataEquipment(skeleton, _logger);
                }
            }

            foreach (var track in GetFragmentsOfType<TrackFragment>())
            {
                if (track.IsPoseAnimation || track.IsProcessed)
                {
                    continue;
                }

                _logger.LogError("WldFileCharacters: Track not assigned: " + track.Name);
            }
        }
    }
}