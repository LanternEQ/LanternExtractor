using System.Collections.Generic;
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

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();
            
            AnimationList = new Dictionary<string, int>();
            
            Tree = new List<SkeletonNode>();
            Meshes = new List<MeshReference>();

            var ba = new BitAnalyzer(flags);

            bool hasUnknownParams = ba.IsBitSet(0);
            bool hasBoundingRadius = ba.IsBitSet(1);
            bool hasMeshReferences = ba.IsBitSet(9);
            
            // Number of bones in the skeleton
            int boneCount = reader.ReadInt32();
            
            // According to Windcatcher, this is a Polygon Animation Reference
            // More research is needed
            int fragment18Reference = reader.ReadInt32();

            if (fragment18Reference != 0)
            {
                _fragment18Reference = fragments[fragment18Reference - 1] as Fragment18;
            }

            // Three sequential DWORDs
            // Unknown purpose
            if (hasUnknownParams)
            {
                reader.BaseStream.Position += 3 * sizeof(int);
            }

            // TODO: Figure out how this bounding radius works
            if (hasBoundingRadius)
            {
                BoundingRadius = reader.ReadSingle();
            }

            Skeleton = new List<SkeletonPieceData>();
            SkeletonPieceDictionary = new Dictionary<string, SkeletonPieceData>();

            // entries - bones
            for (int i = 0; i < boneCount; ++i)
            {
                var piece = new SkeletonPieceData();
                var pieceNew = new SkeletonNode();

                pieceNew.Index = i;

                // Create the skeleton structure
                // refers to this or another 0x10 fragment - confusing
                int entryNameRef = reader.ReadInt32();

                piece.Name = stringHash[-entryNameRef];
                pieceNew.Name = piece.Name;

                // usually 0
                int entryFlags = reader.ReadInt32();
                pieceNew.Flags = entryFlags;

                // reference to an 0x13
                int entryFrag = reader.ReadInt32();
                pieceNew.Track = fragments[entryFrag - 1] as TrackFragment;

                piece.AnimationTracks = new Dictionary<string, TrackFragment>();

                string animName = "POS";
                
                piece.AnimationTracks[animName] = fragments[entryFrag - 1] as TrackFragment;

                int frames = (fragments[entryFrag - 1] as TrackFragment).TrackDefFragment.Frames2.Count;

                if (!AnimationList.ContainsKey(animName))
                {
                    AnimationList[animName] = 1;
                }

                if (frames > AnimationList[animName])
                {
                    AnimationList[animName] = frames;
                }

                // Mesh reference which defines the mesh that should be instantiated with this bone
                int meshReferenceIndex = reader.ReadInt32();

                // The range check is needed as it sometimes references something out of bounds
                // Possible that it's a string hash index reference in this case
                if (meshReferenceIndex > 0 && meshReferenceIndex <= fragments.Count)
                {
                    pieceNew.MeshReference = fragments[meshReferenceIndex - 1] as MeshReference;
                }

                int childrenCount = reader.ReadInt32();

                List<int> moose = new List<int>();
                pieceNew.Children = new List<int>();

                for (int j = 0; j < childrenCount; ++j)
                {
                    int childIndex = reader.ReadInt32();
                    moose.Add(childIndex);
                    pieceNew.Children.Add(childIndex);
                }
                
                pieceNew.Tracks = new Dictionary<string, TrackFragment>();
                
                Tree.Add(pieceNew);
                
                piece.ConnectedPieces = moose;

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