using System;
using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Helpers;
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

        public Fragment18 _fragment18Reference;

        public string ModelBase;
        
        private Dictionary<string, SkeletonPieceData> SkeletonPieceDictionary { get; set; }
        
        // Mapping of bone names
        private Dictionary<string, SkeletonPieceData> SkeletonPieceDictionary2 { get; set; }

        public Dictionary<string, Animation2> Animations = new Dictionary<string, Animation2>();
        
        public Dictionary<int, string> _boneNameMapping = new Dictionary<int, string>();
        
        public float BoundingRadius;

        public List<Mesh> AdditionalMeshes = new List<Mesh>();

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            
            Tree = new List<SkeletonNode>();
            Meshes = new List<MeshReference>();
            Skeleton = new List<SkeletonPieceData>();
            SkeletonPieceDictionary = new Dictionary<string, SkeletonPieceData>();
            SkeletonPieceDictionary2 = new Dictionary<string, SkeletonPieceData>();

            _boneNameMapping[0] = "ROOT";
            
            var reader = new BinaryReader(new MemoryStream(data));

            // Name is (OBJECT)_HS_DEF
            Name = stringHash[-reader.ReadInt32()];

            ModelBase = FragmentNameCleaner.CleanName(this, true);

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

            if (fragment18Reference > 0)
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

                // Always 0 for object bones
                // Confirmed
                int boneFlags = reader.ReadInt32();

                if (boneFlags != 0)
                {
                    
                }

                // Reference to a bone track
                // Confirmed - is never a bad reference
                int trackReferenceIndex = reader.ReadInt32();

                TrackFragment track = fragments[trackReferenceIndex - 1] as TrackFragment;
                AddTrackData(track, true);
                pieceNew.Track = track;
                
                piece.Name = pieceNew.Track.PieceName;
                pieceNew.Name = pieceNew.Track.PieceName;
                _boneNameMapping[i] = pieceNew.Track.PieceName;

                pieceNew.Track.IsProcessed = true;
                
                piece.AnimationTracks = new Dictionary<string, TrackFragment>();

                if (pieceNew.Track == null)
                {
                    logger.LogError("Unable to link track reference!");
                }

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
                
                Tree.Add(pieceNew);
                
                piece.ConnectedPieces = children;

                Skeleton.Add(piece);

                if (piece.Name != "")
                {
                    if (!SkeletonPieceDictionary.ContainsKey(piece.Name))
                    {
                        SkeletonPieceDictionary.Add(piece.Name, piece);
                    }

                    string partName = piece.Name.Replace("_DAG", string.Empty);
                    // remove the modelname
                   // partName = partName.Substring(3, partName.Length - 3);

                    if (partName == string.Empty)
                    {
                        continue;
                    }

                    SkeletonPieceDictionary2[partName] = piece;
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

                    if (meshRef == null)
                    {
                        continue;
                    }
                    
                    // If this is not the first mesh, it's a secondary mesh and we need to determine the attach point
                    Meshes.Add(meshRef);
                    
                    if (FragmentNameCleaner.CleanName(meshRef.Mesh) != ModelBase)
                    {
                        AdditionalMeshes.Add(meshRef.Mesh);
                    }
                    
                    meshRef.Mesh.IsHandled = true;
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
                animationName = cleanedName.Substring(0, 3);
                cleanedName = cleanedName.Remove(0, 3);
                modelName = cleanedName.Substring(0, 3);
                cleanedName = cleanedName.Remove(0, 3);
                pieceName = cleanedName;
            }

            track.SetTrackData(modelName, animationName, pieceName);
            
            if (!Animations.ContainsKey(track.AnimationName))
            {
                Animations[track.AnimationName] = new Animation2();
            }
            
            Animations[track.AnimationName].AddTrack(track);
        }
        
        private void BuildSkeletonTreeData(int index, List<SkeletonNode> treeNodes, string runningName, string runningIndex,
            Dictionary<int, string> paths)
        {
            SkeletonNode currentNode = treeNodes[index];
            
            if (currentNode.Name != string.Empty)
            {
                runningIndex += currentNode.Index + "/";
            }

            runningName += currentNode.Name;

            currentNode.FullPath = runningName;
            
            if (currentNode.Children.Count == 0)
            {
                return;
            }

            runningName += "/";

            foreach (var childNode in currentNode.Children)
            {
                BuildSkeletonTreeData(childNode, treeNodes, runningName, runningIndex, paths);
            }
        }

        public void AddAdditionalMesh(Mesh mesh)
        {
            AdditionalMeshes.Add(mesh);
        }
    }

    public class Animation2
    {
        public string AnimModelBase;
        public Dictionary<string, TrackFragment> Tracks;
        public int FrameCount;
        public int AnimationTimeMs { get; set; }

        public Animation2()
        {
            Tracks = new Dictionary<string, TrackFragment>();
        }

        public void AddTrack(TrackFragment track)
        {
            string trackName = track.Name;

            Tracks[track.PieceName] = track;

            if (string.IsNullOrEmpty(AnimModelBase) &&
                !string.IsNullOrEmpty(track.ModelName))
            {
                AnimModelBase = track.ModelName;
            }
             
            if (track.TrackDefFragment.Frames2.Count > FrameCount)
            {
                FrameCount = track.TrackDefFragment.Frames2.Count;
            }

            int totalTime = track.TrackDefFragment.Frames2.Count * track.FrameMs;

            if (totalTime > AnimationTimeMs)
            {
                AnimationTimeMs = totalTime;
            }
        }
    }
}