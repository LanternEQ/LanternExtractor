using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public List<Mesh> Meshes { get; private set; }
        
        public List<SkeletonNode> Tree { get; set; }

        public Fragment18 _fragment18Reference;

        public string ModelBase;
        
        private Dictionary<string, SkeletonPieceData> SkeletonPieceDictionary { get; set; }
        
        public Dictionary<string, Animation> Animations = new Dictionary<string, Animation>();
        
        public Dictionary<int, string> _boneNameMapping = new Dictionary<int, string>();
        
        public float BoundingRadius;

        public List<Mesh> HelmMeshes = new List<Mesh>();

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            
            Tree = new List<SkeletonNode>();
            Meshes = new List<Mesh>();
            Skeleton = new List<SkeletonPieceData>();
            SkeletonPieceDictionary = new Dictionary<string, SkeletonPieceData>();

            _boneNameMapping[0] = "root";
            
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

                string boneName = string.Empty;

                if (stringHash.ContainsKey(-boneNameIndex))
                {
                    boneName = stringHash[-boneNameIndex];

                    if (boneName.Length != 0)
                    {
                        boneName = boneName.ToLower();

                        if (boneName.StartsWith(ModelBase))
                        {
                            boneName = boneName.Substring(ModelBase.Length);
                        }
                        
                        boneName = boneName.Replace("_dag", string.Empty);

                        if (boneName == string.Empty)
                        {
                            boneName = "root";
                        }
                    }
                }
                
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
                AddPoseTrack(track, boneName);
                pieceNew.Track = track;
                
                piece.Name = boneName;
                pieceNew.Name = boneName;
                _boneNameMapping[i] = boneName;

                pieceNew.Track.IsPoseAnimation = true;
                
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

                    if (pieceNew.Name == "root")
                    {
                        pieceNew.Name = FragmentNameCleaner.CleanName(pieceNew.MeshReference.Mesh);
                    }
                    
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

                    if (meshRef.Mesh == null)
                    {
                        logger.LogError("SkeletonHierarchy: Null reference to mesh for skeleton: " + Name);
                        continue;
                    }

                    if (Meshes.All(x => x.Name != meshRef.Mesh.Name))
                    {
                        Meshes.Add(meshRef.Mesh);
                        meshRef.Mesh.IsHandled = true;
                    }
                }
                
                Meshes = Meshes.OrderBy(x => x.Name).ToList();

                List<int> things = new List<int>();
                
                for (int i = 0; i < size2; ++i)
                {
                    things.Add(reader.ReadInt32());
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

        private void AddPoseTrack(TrackFragment track, string pieceName)
        {
            if (!Animations.ContainsKey("pos"))
            {
                Animations["pos"] = new Animation();
            }
            
            Animations["pos"].AddTrack(track, pieceName);
            track.TrackDefFragment.IsAssigned = true;
            track.IsProcessed = true;
            track.IsPoseAnimation = true;
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
            
            Animations[track.AnimationName].AddTrack(track, track.PieceName);
            track.TrackDefFragment.IsAssigned = true;
            track.IsProcessed = true;
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
    }
}