using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;
using System.Collections.Generic;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileEquipment : WldFile
    {
        public WldFileEquipment(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            List<WldFile> wldFilesToInject = null) : base(wldFile, zoneName, type, logger, settings, wldFilesToInject)
        {
        }

        public override void ExportData()
        {
            base.ExportData();
            ExportParticleSystems();
        }

        protected override void ProcessData()
        {
            base.ProcessData();
            FindUnhandledSkeletons();
            FindAdditionalAnimations();
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

        private void FindUnhandledSkeletons()
        {
            var skeletons = GetFragmentsOfType<SkeletonHierarchy>();
            
            if (skeletons == null)
            {
                return;
            }

            foreach (SkeletonHierarchy skeleton in skeletons)
            {
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