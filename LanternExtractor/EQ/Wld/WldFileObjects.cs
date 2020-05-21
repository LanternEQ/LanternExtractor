using System.IO;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileObjects : WldFile
    {
        public WldFileObjects(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(
            wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }

        protected override void ExportData()
        {
            base.ExportData();
            ExportZoneObjectData();
            //ExportAnimations();
        }

        /// <summary>
        /// Export zone object meshes to .obj files and collision meshes if there are non-solid polygons
        /// Additionally, it exports a list of vertex animated instances
        /// </summary>
        private void ExportZoneObjectData()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                _logger.LogWarning("Cannot export zone object meshes. No meshes found.");
                return;
            }

            ObjectListExporter objectListExporter =
                new ObjectListExporter(_fragmentTypeDictionary[FragmentType.Mesh].Count);

            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
            string rootExportFolder = _zoneName + "/";
            
            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                objectListExporter.AddFragmentData(fragment);

                string meshName = FragmentNameCleaner.CleanName(fragment);

                if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
                {
                    MeshIntermediateExporter meshExporter = new MeshIntermediateExporter(_settings.ExportZoneMeshGroups);
                    meshExporter.AddFragmentData(fragment);
                    meshExporter.WriteAssetToFile(objectsExportFolder + meshName + ".txt");
                }    
                else if (_settings.ModelExportFormat == ModelExportFormat.Obj)
                {
                    MeshObjExporter meshExporter = new MeshObjExporter(ObjExportType.Textured,
                        _settings.ExportHiddenGeometry, false, meshName);
                    MeshObjExporter collisionMeshExport = new MeshObjExporter(ObjExportType.Collision,
                        _settings.ExportHiddenGeometry, false, meshName);
                    meshExporter.AddFragmentData(fragment);
                    collisionMeshExport.AddFragmentData(fragment);

                    meshExporter.WriteAssetToFile(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                    meshExporter.WriteAllFrames(objectsExportFolder + meshName + LanternStrings.ObjFormatExtension);
                    meshExporter.WriteAssetToFile(objectsExportFolder + meshName + "_collision" +
                                                  LanternStrings.ObjFormatExtension);
                }
            }

            objectListExporter.WriteAssetToFile(rootExportFolder + _zoneName + "_objects.txt");

            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                string listName = FragmentNameCleaner.CleanName(fragment);

                if (_settings.ModelExportFormat == ModelExportFormat.Intermediate)
                {
                    MeshIntermediateMaterialsExport mtlExporter =
                        new MeshIntermediateMaterialsExport(_settings, _zoneName);
                    mtlExporter.AddFragmentData(fragment);
                    mtlExporter.WriteAssetToFile(objectsExportFolder + listName + "_materials.txt");
                }
                else
                {
                    MeshObjMtlExporter mtlExporter = new MeshObjMtlExporter(_settings, _zoneName);
                    mtlExporter.AddFragmentData(fragment);
                    mtlExporter.WriteAssetToFile(objectsExportFolder + listName + LanternStrings.FormatMtlExtension);
                }
            }
        }

        private void ExportAnimations()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.TrackFragment))
            {
                return;
            }
            
            string folder = _zoneName + "/Objects/Animations/";

            Directory.CreateDirectory(folder);


            foreach (var fragment in _fragmentTypeDictionary[FragmentType.TrackFragment])
            {
                TrackFragment track = fragment as TrackFragment;

                if (track == null)
                {
                    continue;
                }

                string trackName = FragmentNameCleaner.CleanName(track);

                foreach (WldFragment modelFragment in _fragmentTypeDictionary[FragmentType.ModelReference])
                {
                    Actor model = modelFragment as Actor;

                    if (model == null)
                    {
                        continue;
                    }

                    // TODO: Can there ever be more than one?
                    if (model.SkeletonReference == null)
                    {
                        continue;
                    }

                    // TODO: Handle more if they exist
                    SkeletonHierarchy skeleton = model.SkeletonReference.SkeletonHierarchy;

                    string skeletonName = FragmentNameCleaner.CleanName(skeleton);

                    if (trackName == skeletonName)
                    {
                        track.IsProcessed = true;
                        skeleton.AddNewTrack(track);
                        break;
                    }

                    foreach (var childBone in skeleton.Tree)
                    {
                        string boneName = childBone.Name.Replace("_DAG", "").ToLower();

                        if (trackName == boneName)
                        {
                            childBone.Track = track;
                            track.IsProcessed = true;
                            skeleton.AddNewTrack(track);
                            /*if (track.TrackDefFragment.Frames2.Count > skyModel.Frames)
                            { 
                                skyModel.Frames = track.TrackDefFragment.Frames2.Count;
                            }*/

                            break;
                        }
                    }
                }

                if (!track.IsProcessed)
                {
                    _logger.LogError("Error processing animation track: " + trackName);
                    continue;
                }
            }
            
            StringBuilder animation = new StringBuilder();

            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.ModelReference])
            {
                Actor modelFragment = fragment as Actor;

                if (modelFragment == null)
                {
                    continue;
                }
                
                if (modelFragment.SkeletonReference == null)
                {
                    continue;
                }

                SkeletonHierarchy skeleton = modelFragment.SkeletonReference.SkeletonHierarchy;

                animation.AppendLine("# Sky Animation Test");
                //animation.AppendLine("framecount," + skyModel.Frames);

                // Iterate through each tree node
                for (int i = 0; i < skeleton.Tree.Count; ++i)
                {
                    var frames = skeleton.Tree[i].Track.TrackDefFragment.Frames2;

                    // Iterate through each frame
                    for (int j = 0; j < frames.Count; ++j)
                    {
                        animation.Append(skeleton.Tree[i].FullPath);
                        animation.Append(",");

                        animation.Append(j);
                        animation.Append(",");
                        
                        int frame = frames.Count == 1 ? j : 0;
                            
                        animation.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.x);
                        animation.Append(",");

                        animation.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.z);
                        animation.Append(",");

                        animation.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.y);
                        animation.Append(",");

                        animation.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.x);
                        animation.Append(",");

                        animation.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.z);
                        animation.Append(",");

                        animation.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.y);
                        animation.Append(",");

                        animation.Append(skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.w);
                        animation.AppendLine();
                    }
                }

                File.WriteAllText(folder + FragmentNameCleaner.CleanName(skeleton) + "_animation.txt",
                    animation.ToString());
                animation.Clear();
            }
        }
    }
}