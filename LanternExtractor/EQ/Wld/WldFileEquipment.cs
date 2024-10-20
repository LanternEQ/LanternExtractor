﻿using LanternExtractor.EQ.Archive;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;
using LanternExtractor.Infrastructure.Settings;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileEquipment : WldFile
    {
        public WldFileEquipment(ArchiveFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
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

                if (!FragmentNameDictionary.ContainsKey(actorName))
                {
                    // Create dummy Actor for exporting equipment which lack an ACTORDEF (ex. IT145)
                    // Technically equipment just needs a HS_DEF and not an ACTORDEF for eqgame to load it
                    AddFragment(new Actor()
                    {
                        Name = actorName
                    });
                }

                (FragmentNameDictionary[actorName] as Actor)?.AssignSkeletonReference(skeleton, Logger);
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
                        Logger.LogError($"Assigning {track.Name} to {skeleton.Name}");
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

                Logger.LogError("WldFileCharacters: Track not assigned: " + track.Name);
            }
        }
    }
}
