using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x10 - Skeleton Hierarchy
    /// Describes the layout of a complete skeleton and which pieces connect to eachother
    /// </summary>
    public class SkeletonHierarchy : WldFragment
    {
        public List<SkeletonPieceData> Skeleton { get; private set; }

        public List<MeshReference> Meshes { get; private set; }
        
        public List<SkeletonNode> Tree { get; set; }

        public Dictionary<string, int> AnimationList;

        public Fragment18 _fragment18Reference;
        
        private Dictionary<string, SkeletonPieceData> SkeletonPieceDictionary { get; set; }

        public float BoundingRadius;

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            AnimationList = new Dictionary<string, int>();
            Tree = new List<SkeletonNode>();
            Meshes = new List<MeshReference>();
            Skeleton = new List<SkeletonPieceData>();
            SkeletonPieceDictionary = new Dictionary<string, SkeletonPieceData>();
            
            var reader = new BinaryReader(new MemoryStream(data));

            // Name is (OBJECT)_HS_DEF
            Name = stringHash[-reader.ReadInt32()];

            // Always 2 when used in main zone, and object files.
            // This means, it has a bounding radius
            // Some differences in character + model archives
            // Confirmed
            int flags = reader.ReadInt32();

            if (flags != 2)
            {
                
            }

            var ba = new BitAnalyzer(flags);

            bool hasUnknownParams = ba.IsBitSet(0);
            bool hasBoundingRadius = ba.IsBitSet(1);
            bool hasMeshReferences = ba.IsBitSet(9);
            
            // Number of bones in the skeleton
            // Confirmed
            int boneCount = reader.ReadInt32();
            
            // Fragment 18 reference
            // Not used for the UFO, used for trees. Let's figure this out.
            // Confirmed
            int fragment18Reference = reader.ReadInt32();

            if (fragment18Reference != 0)
            {
                _fragment18Reference = fragments[fragment18Reference - 1] as Fragment18;
            }

            // Three sequential DWORDs
            // This will never be hit for object animations.
            // Confirmed
            if (hasUnknownParams)
            {
                reader.BaseStream.Position += 3 * sizeof(int);
            }

            // This is the sphere radius checked against the frustum to cull this object
            // Confirmed we can see this exact in game
            if (hasBoundingRadius)
            {
                BoundingRadius = reader.ReadSingle();
            }

            // Read in each bone
            for (int i = 0; i < boneCount; ++i)
            {
                var piece = new SkeletonPieceData();
                var pieceNew = new SkeletonNode();

                pieceNew.Index = i;

                // An index into the string has to get this bone's name
                int boneNameIndex = reader.ReadInt32();

                piece.Name = stringHash[-boneNameIndex];
                pieceNew.Name = stringHash[-boneNameIndex];

                // Always 0 for object bones
                // Confirmed
                int boneFlags = reader.ReadInt32();
                pieceNew.Flags = boneFlags;

                if (boneFlags != 0)
                {
                    
                }

                // Reference to a bone track
                // Confirmed - is never a bad reference
                int trackReferenceIndex = reader.ReadInt32();
                pieceNew.Track = fragments[trackReferenceIndex - 1] as TrackFragment;

                piece.AnimationTracks = new Dictionary<string, TrackFragment>();

                if (pieceNew.Track == null)
                {
                    logger.LogError("Unable to link track reference!");
                }

                string animName = "POS";
                piece.AnimationTracks[animName] = fragments[trackReferenceIndex - 1] as TrackFragment;
                int frames = (fragments[trackReferenceIndex - 1] as TrackFragment).TrackDefFragment.Frames2.Count;
                if (!AnimationList.ContainsKey(animName))
                {
                    AnimationList[animName] = 1;
                }

                if (frames > AnimationList[animName])
                {
                    AnimationList[animName] = frames;
                }

                // If it's a negative number, it's a string hash reference. 
                // The UFO has two but they are just related to the beam:
                // BEAMP2_PCD, BEAMP1_PCD
                // If it's a positive number, it's a mesh reference reference
                // Confirmed
                int meshReferenceIndex = reader.ReadInt32();
                
                if (meshReferenceIndex < 0)
                {
                    string name = stringHash[-meshReferenceIndex];
                }
                else if (meshReferenceIndex != 0)
                {
                    pieceNew.MeshReference = fragments[meshReferenceIndex - 1] as MeshReference;

                    // Never null
                    // Confirmed
                    if (pieceNew.MeshReference == null)
                    {
                        logger.LogError("Mesh reference null");
                    }
                }

                // The number of children
                // These could be int16 but I think they are int32
                int childrenCount = reader.ReadInt32();

                List<int> children = new List<int>();
                pieceNew.Children = new List<int>();

                for (int j = 0; j < childrenCount; ++j)
                {
                    int childIndex = reader.ReadInt32();
                    children.Add(childIndex);
                    pieceNew.Children.Add(childIndex);
                }
                
                pieceNew.Tracks = new Dictionary<string, TrackFragment>();
                
                Tree.Add(pieceNew);
                
                piece.ConnectedPieces = children;

                Skeleton.Add(piece);

                if (piece.Name != "")
                {
                    if (!SkeletonPieceDictionary.ContainsKey(piece.Name))
                    {
                        SkeletonPieceDictionary.Add(piece.Name, piece);
                    }
                }
            }

            // Read in mesh references
            // These are never used in object animation
            if (hasMeshReferences)
            {
                int size2 = reader.ReadInt32();
                
                for (int i = 0; i < size2; ++i)
                {
                    int meshRefIndex = reader.ReadInt32();

                    MeshReference meshRef = fragments[meshRefIndex - 1] as MeshReference;

                    if (meshRef != null)
                    {
                        // If this is not the first mesh, it's a secondary mesh and we need to determine the attach point
                         Meshes.Add(meshRef);
                    }
                }
            }
            
            BuildSkeletonTreeData(0, Tree, string.Empty, string.Empty, new Dictionary<int, string>());
            
            // Confirmed end for objects
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x10: Skeleton pieces: " + Skeleton.Count);
        }

        public void AddNewTrack(TrackFragment newTrack)
        {
            string animationName = newTrack.Name.Substring(0, 3);
            string boneName = newTrack.Name.Substring(3);
            boneName = boneName.Substring(0, boneName.Length - 6) + "_DAG";

            if (!SkeletonPieceDictionary.ContainsKey(boneName))
                return;

            SkeletonPieceData piece = SkeletonPieceDictionary[boneName];

            piece.AnimationTracks[animationName] = newTrack;
        }
        
        private void BuildSkeletonTreeData(int index, List<SkeletonNode> treeNodes, string runningName, string runningIndex,
            Dictionary<int, string> paths)
        {
            SkeletonNode currentNode = treeNodes[index];

            if (currentNode.Name != string.Empty)
            {
                string fixedName = currentNode.Name.Replace("_DAG", "");

                if (fixedName.Length >= 3)
                {
                    runningName += currentNode.Name.Replace("_DAG", "") + "/";
                }
            }

            if (currentNode.Name != string.Empty)
            {
                runningIndex += currentNode.Index + "/";
            }

            if (runningName.Length >= 1)
            {
                currentNode.FullPath = runningName.Substring(0, runningName.Length - 1);
            }

            if (runningIndex.Length >= 1)
            {
                currentNode.FullIndexPath = runningIndex.Substring(0, runningIndex.Length - 1);
            }

            if (currentNode.Children.Count == 0)
            {
                return;
            }

            foreach (var childNode in currentNode.Children)
            {
                BuildSkeletonTreeData(childNode, treeNodes, runningName, runningIndex, paths);
            }
        }
    }
}