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
            CheckOrphanedMeshes();
            FindUnhandledSkeletons();
            FindAdditionalAnimations();
            base.ExportData();
            ExportMeshList();
            ExportParticleSystems();
            ExportSkeletonsNew();
            //ExportActorsNew();
        }

        private void DoAllSkeletons()
        {
            var skeletons = GetFragmentsOfType2<SkeletonHierarchy>();

            foreach (var skeleton in skeletons)
            {
                skeleton.BuildSkeletonData(false);
            }
        }

        private void ExportParticleSystems()
        {
            var particles = GetFragmentsOfType2<ParticleCloud>();

            foreach (var particle in particles)
            {
                var writer = new ParticleSystemWriter();
                writer.AddFragmentData(particle);
                writer.WriteAssetToFile(GetRootExportFolder() + "/Particles/" + FragmentNameCleaner.CleanName(particle) + ".txt");
            }
        }
        
        private void ExportSkeletonsNew()
        {
            var skeletons = GetFragmentsOfType2<SkeletonHierarchy>();

            foreach (var skeleton in skeletons)
            {
                var writer = new SkeletonHierarchyNewWriter();
                writer.AddFragmentData(skeleton);
                writer.WriteAssetToFile(GetRootExportFolder() + "/SkeletonsNew/" + FragmentNameCleaner.CleanName(skeleton) + ".txt");
            }
        }
        
        private void ExportActorsNew()
        {
            var actors = GetFragmentsOfType2<Actor>();

            foreach (var actor in actors)
            {
                var writer = new ActorWriterNew();
                writer.AddFragmentData(actor);
                writer.WriteAssetToFile(GetRootExportFolder() + "/Actor/" + FragmentNameCleaner.CleanName(actor) + ".txt");
            }
        }

        private void FindUnhandledSkeletons()
        {
            var skeletons = GetFragmentsOfType(FragmentType.SkeletonHierarchy);
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

                (_fragmentNameDictionary[actorName] as Actor)?.AssignSkeletonReference(skeleton);
            }
        }

        private void FindAdditionalAnimations()
        {
            var animations = GetFragmentsOfType2<TrackFragment>();
            var skeletons = GetFragmentsOfType2<SkeletonHierarchy>();

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

                        if (track.Name == "C05IT153MAIN05_TRACK")
                        {
                            
                        }
                        
                        skeleton.AddTrackDataEquipment(track, boneName.ToLower());
                    }
                }

                if (!track.IsNameParsed)
                {
                    //_logger.LogError($"Assigning {track.Name} to {skeleton.Name}");
                    //track.ParseTrackDataEquipment(skeleton, _logger);
                }
            }

            foreach (var trackFragment in _fragmentTypeDictionary[FragmentType.TrackFragment])
            {
                TrackFragment track = trackFragment as TrackFragment;

                if (track == null)
                {
                    continue;
                }
                
                if (track.IsPoseAnimation || track.IsProcessed)
                {
                    continue;
                }

                _logger.LogError("WldFileCharacters: Track not assigned: " + track.Name);
            }
        }

        private void CheckOrphanedMeshes()
        {
            if(_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                foreach (var meshReferenceFragment in _fragmentTypeDictionary[FragmentType.Mesh])
                {
                    Mesh mesh = meshReferenceFragment as Mesh;

                    if (mesh == null)
                    {
                        continue;
                    }

                    if (mesh.IsHandled)
                    {
                        continue;
                    }

                    string cleanedName = FragmentNameCleaner.CleanName(mesh);
                    //_logger.LogError("ORPHANED: " + cleanedName);
                }
            }
        }

        private void ExportModels()
        {
            return;
        }
    }
}