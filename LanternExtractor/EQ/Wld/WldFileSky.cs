using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Exporters;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileSky : WldFile
    {
        private List<SkyModel> _models = new List<SkyModel>();

        public WldFileSky(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings,
            WldFile wldToInject = null) : base(wldFile, zoneName, type, logger, settings, wldToInject)
        {
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportData()
        {
            base.ExportData();
            
            ExportSkyMeshes();

            foreach (var skeletonFragment in _fragmentTypeDictionary[FragmentType.HierSpriteFragment])
            {
                HierSpriteDefFragment skeleton = skeletonFragment as HierSpriteDefFragment;

                if(skeleton == null)
                {
                    continue;
                }
                
                Dictionary<int, string> boneNames = new Dictionary<int, string>();
                RecurseBone(0, skeleton.Tree, string.Empty, string.Empty, boneNames);
            }
            
            ExportSkySkeletons();
            ExportSkyAnimations();
        }

        private void ExportSkySkeletons()
        {
            string folder = "sky/Skeletons/";

            Directory.CreateDirectory(folder);
            
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.HierSpriteFragment))
            {
                return;
            }

            var skeletonFragments = _fragmentTypeDictionary[FragmentType.HierSpriteFragment];

            foreach (var fragment in skeletonFragments)
            {
                var skeleton = fragment as HierSpriteDefFragment;

                if (skeleton == null)
                {
                    continue;
                }

                _models.Add(new SkyModel() {Skeleton = skeleton});
                
                StringBuilder miniTree = new StringBuilder();
                miniTree.AppendLine("# Sky Skeleton Test");

                for (var i = 0; i < skeleton.Tree.Count; i++)
                {
                    var node = skeleton.Tree[i];

                    string childString = string.Empty;
                    foreach (var children in node.Children)
                    {
                        childString += children;

                        if (children != node.Children.Last())
                        {
                            childString += ";";
                        }
                    }
                    
                    miniTree.AppendLine(node.FullPath.Replace("_DAG", "") + (childString == string.Empty ? "" : "," + childString));
                }

                File.WriteAllText(folder + skeleton.Name.Replace("_HS_DEF", string.Empty) + "_skeleton" + ".txt", miniTree.ToString());
            }
        }

        private void RecurseBone(int index, List<SkeletonNode> treeNodes, string runningName, string runningIndex,
            Dictionary<int, string> paths)
        {
            SkeletonNode node = treeNodes[index];

            if (node.Name != string.Empty)
            {
                string fixedName = node.Name.Replace("_DAG", "");

                if (fixedName.Length >= 3)
                {
                    runningName += node.Name.Replace("_DAG", "") + "/";
                }
            }

            if (node.Name != string.Empty)
            {
                runningIndex += node.Index + "/";
            }

            if (runningName.Length >= 1)
            {
                node.FullPath = runningName.Substring(0, runningName.Length - 1);
            }

            if (runningIndex.Length >= 1)
            {
                node.FullIndexPath = runningIndex.Substring(0, runningIndex.Length - 1);
            }

            if (node.Children.Count == 0)
            {
                return;
            }

            foreach (var childNode in node.Children)
            {
                RecurseBone(childNode, treeNodes, runningName, runningIndex, paths);
            }
        }

        private void ExportSkyAnimations()
        {
            string folder = "sky/Animations/";

            Directory.CreateDirectory(folder);
            
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.TrackFragment))
            {
                return;
            }

            foreach (var fragment in _fragmentTypeDictionary[FragmentType.TrackFragment])
            {
                TrackFragment track = fragment as TrackFragment;

                if (track == null)
                {
                    continue;
                }
                
                string trackName = track.TrackDefFragment.Name.Replace("_TRACKDEF", "");

                foreach (var skyModel in _models)
                {
                    var skeleton = skyModel.Skeleton;

                    if (skeleton == null)
                    {
                        continue;
                    }

                    string skeletonName = skeleton.Name.Replace("_HS_DEF", "");

                    if (skeletonName == trackName)
                    {
                        track.IsProcessed = true;
                        skeleton.Tree[0].Track = track;
                        skyModel.RootName = skeletonName;
                        break;
                    }

                    foreach (var childBone in skeleton.Tree)
                    {
                        if (trackName == childBone.Name.Replace("_DAG", ""))
                        {
                            childBone.Track = track;
                            track.IsProcessed = true;

                            if (track.TrackDefFragment.Frames2.Count > skyModel.Frames)
                            { 
                                skyModel.Frames = track.TrackDefFragment.Frames2.Count;
                            }
                            break;
                        }
                    }
                }

                if (!track.IsProcessed)
                {
                    
                }
            }
            
            StringBuilder animation = new StringBuilder();
            
            foreach (var skyModel in _models)
            {
                animation.AppendLine("# Sky Animation Test");
                animation.AppendLine("framecount," + skyModel.Frames);
                for (int i = 0; i < skyModel.Skeleton.Tree.Count; ++i)
                {
                    for (int j = 0; j < skyModel.Frames; ++j)
                    {
                        animation.Append(skyModel.Skeleton.Tree[i].FullPath);
                        animation.Append(",");
                        
                        animation.Append(j);
                        animation.Append(",");

                        int frame = j;
                        
                        if (skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2.Count == 1)
                        {
                            frame = 0;
                        }
                        
                        animation.Append(skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.x);
                        animation.Append(",");
                        
                        animation.Append(skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.z);
                        animation.Append(",");

                        animation.Append(skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Translation.y);                        
                        animation.Append(",");

                        animation.Append(skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.x);
                        animation.Append(",");

                        animation.Append(skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.z);
                        animation.Append(",");

                        animation.Append(skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.y);
                        animation.Append(",");

                        animation.Append(skyModel.Skeleton.Tree[i].Track.TrackDefFragment.Frames2[frame].Rotation.w);
                        animation.AppendLine();
                    }
                }

                File.WriteAllText(folder + skyModel.RootName + "_animation.txt", animation.ToString());
                animation.Clear();
            }
        }

        /// <summary>
        /// Exports the zone meshes to an .obj file
        /// This includes the textures mesh, the collision mesh, and the water and lava meshes (if they exist)
        /// </summary>
        private void ExportSkyMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                _logger.LogWarning("Cannot export zone meshes. No meshes found.");
                return;
            }
            
            string zoneExportFolder = _zoneName + "/" + LanternStrings.ExportZoneFolder;

            MeshObjExporter exporter = new MeshObjExporter(ObjExportType.Textured, false, "sky");

            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.Mesh])
            {
                exporter.AddFragmentData(fragment);
            }
            
            exporter.WriteAssetToFile(zoneExportFolder + _zoneName + LanternStrings.ObjFormatExtension);
            
            MeshObjMtlExporter mtlExporter = new MeshObjMtlExporter(_settings, _zoneName);

            foreach (WldFragment fragment in _fragmentTypeDictionary[FragmentType.MaterialList])
            {
                mtlExporter.AddFragmentData(fragment);
            }
            
            mtlExporter.WriteAssetToFile(zoneExportFolder + _zoneName + LanternStrings.FormatMtlExtension);
        }
    }
}