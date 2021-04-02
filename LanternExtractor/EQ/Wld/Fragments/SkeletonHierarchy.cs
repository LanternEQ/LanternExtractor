using System;
using System.Collections.Generic;
using System.Linq;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// SkeletonHierarchy (0x10)
    /// Internal name: _HS_DEF
    /// Describes the layout of a complete skeleton and which pieces connect to eachother
    /// </summary>
    public class SkeletonHierarchy : WldFragment
    {
        public List<SkeletonNode> Skeleton { get; private set; }

        public List<Mesh> Meshes { get; private set; }
        public List<LegacyMesh> AlternateMeshes { get; private set; }

        public List<SkeletonNode> Tree { get; set; }

        public Fragment18 _fragment18Reference;

        public string ModelBase;

        public bool IsAssigned { get; set; }

        private Dictionary<string, SkeletonNode> SkeletonPieceDictionary { get; set; }

        public Dictionary<string, Animation> Animations = new Dictionary<string, Animation>();

        public Dictionary<int, string> BoneMappingClean = new Dictionary<int, string>();
        public Dictionary<int, string> BoneMapping = new Dictionary<int, string>();

        public float BoundingRadius;

        public List<Mesh> HelmMeshes = new List<Mesh>();

        public override void Initialize(int index, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, size, data, fragments, stringHash, isNewWldFormat, logger);

            Tree = new List<SkeletonNode>();
            Meshes = new List<Mesh>();
            AlternateMeshes = new List<LegacyMesh>();
            Skeleton = new List<SkeletonNode>();
            SkeletonPieceDictionary = new Dictionary<string, SkeletonNode>();

            Name = stringHash[-Reader.ReadInt32()];
            ModelBase = FragmentNameCleaner.CleanName(this, true);

            // Always 2 when used in main zone, and object files.
            // This means, it has a bounding radius
            // Some differences in character + model archives
            // Confirmed
            int flags = Reader.ReadInt32();

            if (flags != 2)
            {
            }

            var ba = new BitAnalyzer(flags);

            bool hasUnknownParams = ba.IsBitSet(0);
            bool hasBoundingRadius = ba.IsBitSet(1);
            bool hasMeshReferences = ba.IsBitSet(9);

            // Number of bones in the skeleton
            // Confirmed
            int boneCount = Reader.ReadInt32();

            // Fragment 18 reference
            // Not used for the UFO, used for trees. Let's figure this out.
            // Confirmed
            int fragment18Reference = Reader.ReadInt32();

            if (fragment18Reference > 0)
            {
                _fragment18Reference = fragments[fragment18Reference - 1] as Fragment18;
            }

            // Three sequential DWORDs
            // This will never be hit for object animations.
            // Confirmed
            if (hasUnknownParams)
            {
                Reader.BaseStream.Position += 3 * sizeof(int);
            }

            // This is the sphere radius checked against the frustum to cull this object
            // Confirmed we can see this exact in game
            if (hasBoundingRadius)
            {
                BoundingRadius = Reader.ReadSingle();
            }

            for (int i = 0; i < boneCount; ++i)
            {
                var pieceNew = new SkeletonNode
                {
                    Index = i
                };

                // An index into the string has to get this bone's name
                int boneNameIndex = Reader.ReadInt32();
                string boneName = string.Empty;
                if (stringHash.ContainsKey(-boneNameIndex))
                {
                    boneName = stringHash[-boneNameIndex];
                }

                // Always 0 for object bones
                // Confirmed
                int boneFlags = Reader.ReadInt32();

                // Reference to a bone track
                // Confirmed - is never a bad reference
                int trackReferenceIndex = Reader.ReadInt32() - 1;

                TrackFragment track = fragments[trackReferenceIndex] as TrackFragment;
                AddPoseTrack(track, boneName);
                pieceNew.Track = track;
                pieceNew.Name = boneName;
                BoneMappingClean[i] = Animation.CleanBoneAndStripBase(boneName, ModelBase);
                BoneMapping[i] = boneName;

                pieceNew.Track.IsPoseAnimation = true;
                pieceNew.AnimationTracks = new Dictionary<string, TrackFragment>();

                if (pieceNew.Track == null)
                {
                    logger.LogError("Unable to link track reference!");
                }

                int meshReferenceIndex = Reader.ReadInt32() - 1;

                if (meshReferenceIndex < 0)
                {
                    string name = stringHash[-meshReferenceIndex - 1];
                }
                else if (meshReferenceIndex != 0)
                {
                    pieceNew.MeshReference = fragments[meshReferenceIndex] as MeshReference;

                    if (pieceNew.MeshReference != null)
                    {
                        if (pieceNew.MeshReference.Mesh != null)
                        {
                        }
                    }
                    else
                    {
                        pieceNew.ParticleCloud = fragments[meshReferenceIndex] as ParticleCloud;
                    }

                    if (pieceNew.Name == "root")
                    {
                        pieceNew.Name = FragmentNameCleaner.CleanName(pieceNew.MeshReference.Mesh);
                    }

                    // Never null
                    // Confirmed
                    if (pieceNew.MeshReference == null && pieceNew.ParticleCloud == null)
                    {
                        logger.LogError("Mesh reference null");
                    }
                }

                int childCount = Reader.ReadInt32();

                pieceNew.Children = new List<int>();

                for (int j = 0; j < childCount; ++j)
                {
                    int childIndex = Reader.ReadInt32();
                    pieceNew.Children.Add(childIndex);
                }

                Tree.Add(pieceNew);
                Skeleton.Add(pieceNew);

                if (pieceNew.Name != "")
                {
                    if (!SkeletonPieceDictionary.ContainsKey(pieceNew.Name))
                    {
                        SkeletonPieceDictionary.Add(pieceNew.Name, pieceNew);
                    }
                }
            }

            // Read in mesh references
            // All meshes will have vertex bone assignments
            if (hasMeshReferences)
            {
                int size2 = Reader.ReadInt32();

                for (int i = 0; i < size2; ++i)
                {
                    int meshRefIndex = Reader.ReadInt32() - 1;

                    MeshReference meshRef = fragments[meshRefIndex] as MeshReference;

                    if (meshRef?.Mesh != null)
                    {
                        if (Meshes.All(x => x.Name != meshRef.Mesh.Name))
                        {
                            Meshes.Add(meshRef.Mesh);
                            meshRef.Mesh.IsHandled = true;
                        }
                    }

                    if (meshRef?.LegacyMesh != null)
                    {
                        if (AlternateMeshes.All(x => x.Name != meshRef.LegacyMesh.Name))
                        {
                            AlternateMeshes.Add(meshRef.LegacyMesh);
                            //meshRef.AlternateMesh.IsHandled = true;
                        }
                    }
                }

                Meshes = Meshes.OrderBy(x => x.Name).ToList();

                List<int> things = new List<int>();

                for (int i = 0; i < size2; ++i)
                {
                    things.Add(Reader.ReadInt32());
                }
            }

            // Confirmed end for objects
            if (Reader.BaseStream.Position != Reader.BaseStream.Length)
            {
            }
        }

        public void BuildSkeletonData(bool stripModelBase)
        {
            BuildSkeletonTreeData(0, Tree, string.Empty, string.Empty, string.Empty, stripModelBase);
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x10: Skeleton pieces: " + Skeleton.Count);
        }

        private void AddPoseTrack(TrackFragment track, string pieceName)
        {
            if (!Animations.ContainsKey("pos"))
            {
                Animations["pos"] = new Animation();
            }

            Animations["pos"].AddTrack(track, pieceName, Animation.CleanBoneName(pieceName), Animation.CleanBoneAndStripBase(pieceName, ModelBase));
            track.TrackDefFragment.IsAssigned = true;
            track.IsProcessed = true;
            track.IsPoseAnimation = true;
        }

        public void AddTrackDataEquipment(TrackFragment track, string boneName, bool isDefault = false)
        {
            if (track.Name.Contains("C05IT153") && track.TrackDefFragment.Frames.Count != 1)
            {
            }

            string animationName = string.Empty;
            string modelName = string.Empty;
            string pieceName = string.Empty;

            string cleanedName = FragmentNameCleaner.CleanName(track, true);

            if (isDefault)
            {
                animationName = "pos";
                modelName = ModelBase;
                cleanedName = cleanedName.Replace(ModelBase, String.Empty);
                pieceName = cleanedName == string.Empty ? "root" : cleanedName;
            }
            else
            {
                if (cleanedName.Length <= 3)
                {
                    return;
                }

                animationName = cleanedName.Substring(0, 3);
                cleanedName = cleanedName.Remove(0, 3);

                if (cleanedName.Length < 3)
                {
                    return;
                }

                modelName = ModelBase;
                pieceName = boneName;

                if (pieceName == string.Empty)
                {
                    pieceName = "root";
                }
            }

            track.SetTrackData(modelName, animationName, pieceName);

            if (Animations.ContainsKey(track.AnimationName))
            {
                if (modelName == ModelBase && ModelBase != Animations[animationName].AnimModelBase)
                {
                    Animations.Remove(animationName);
                }

                if (modelName != ModelBase && ModelBase == Animations[animationName].AnimModelBase)
                {
                    return;
                }
            }

            if (!Animations.ContainsKey(track.AnimationName))
            {
                Animations[track.AnimationName] = new Animation();
            }

            Animations[track.AnimationName].AddTrack(track, track.PieceName, Animation.CleanBoneName(track.PieceName),
                Animation.CleanBoneAndStripBase(track.PieceName, ModelBase));
            track.TrackDefFragment.IsAssigned = true;
            track.IsProcessed = true;
        }

        public void AddTrackData(TrackFragment track, bool isDefault = false)
        {
            string animationName = string.Empty;
            string modelName = string.Empty;
            string pieceName = string.Empty;

            string cleanedName = FragmentNameCleaner.CleanName(track, true);

            if (isDefault)
            {
                animationName = "pos";
                modelName = ModelBase;
                cleanedName = cleanedName.Replace(ModelBase, String.Empty);
                pieceName = cleanedName == string.Empty ? "root" : cleanedName;
            }
            else
            {
                if (cleanedName.Length <= 3)
                {
                    return;
                }

                animationName = cleanedName.Substring(0, 3);
                cleanedName = cleanedName.Remove(0, 3);

                if (cleanedName.Length < 3)
                {
                    return;
                }

                modelName = cleanedName.Substring(0, 3);
                cleanedName = cleanedName.Remove(0, 3);
                pieceName = cleanedName;

                if (pieceName == string.Empty)
                {
                    pieceName = "root";
                }
            }

            track.SetTrackData(modelName, animationName, pieceName);

            if (Animations.ContainsKey(track.AnimationName))
            {
                if (modelName == ModelBase && ModelBase != Animations[animationName].AnimModelBase)
                {
                    Animations.Remove(animationName);
                }

                if (modelName != ModelBase && ModelBase == Animations[animationName].AnimModelBase)
                {
                    return;
                }
            }

            if (!Animations.ContainsKey(track.AnimationName))
            {
                Animations[track.AnimationName] = new Animation();
            }

            Animations[track.AnimationName]
                .AddTrack(track, track.PieceName, Animation.CleanBoneName(track.PieceName),
                    Animation.CleanBoneAndStripBase(track.PieceName, ModelBase));
            track.TrackDefFragment.IsAssigned = true;
            track.IsProcessed = true;
        }

        private void BuildSkeletonTreeData(int index, List<SkeletonNode> treeNodes, string runningName,
            string runningNameCleaned,
            string runningIndex, bool stripModelBase)
        {
            // TODO: rename to bone
            SkeletonNode node = treeNodes[index];
            node.CleanedName = CleanBoneName(node.Name, stripModelBase);
            BoneMappingClean[index] = node.CleanedName;

            if (node.Name != string.Empty)
            {
                runningIndex += node.Index + "/";
            }

            runningName += node.Name;
            runningNameCleaned += node.CleanedName;

            node.FullPath = runningName;
            node.CleanedFullPath = runningNameCleaned;

            if (node.Children.Count == 0)
            {
                return;
            }

            runningName += "/";
            runningNameCleaned += "/";

            foreach (var childNode in node.Children)
            {
                BuildSkeletonTreeData(childNode, treeNodes, runningName, runningNameCleaned, runningIndex,
                    stripModelBase);
            }
        }

        private string CleanBoneName(string nodeName, bool stripModelBase)
        {
            nodeName = nodeName.Replace("_DAG", "");
            nodeName = nodeName.ToLower();
            if (stripModelBase)
            {
                nodeName = nodeName.Replace(ModelBase, string.Empty);
            }

            nodeName += nodeName.Length == 0 ? "root" : string.Empty;
            return nodeName;
        }

        public void AddAdditionalMesh(Mesh mesh)
        {
            if (Meshes.Any(x => x.Name == mesh.Name))
            {
                return;
            }

            if (HelmMeshes.Any(x => x.Name == mesh.Name))
            {
                return;
            }

            if (mesh.MobPieces.Count == 0)
            {
                return;
            }

            HelmMeshes.Add(mesh);
        }

        public bool IsValidSkeleton(string trackName, out string boneName)
        {
            string track = trackName.Substring(3);

            if (trackName == ModelBase)
            {
                boneName = ModelBase;
                return true;
            }

            foreach (var bone in Tree)
            {
                string cleanBoneName = bone.Name.Replace("_DAG", string.Empty).ToLower();
                if (cleanBoneName == track)
                {
                    boneName = bone.Name.ToLower();
                    return true;
                }
            }

            boneName = string.Empty;
            return false;
        }
    }
}