using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// sforea  0x10 - Skeleton Track
    /// Describes the layout of a complete skeleton and which pieces connect to eachother
    /// </summary>
    public class HierSpriteDefFragment : WldFragment
    {
        public List<SkeletonPieceData> Skeleton { get; private set; }

        public List<MeshReference> Meshes { get; private set; }
        
        public List<SkeletonNode> Tree { get; set; }
        
        public Dictionary<string, SkeletonPieceData> SkeletonPieceDictionary { get; private set; }

        public float BoundingRadius;

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            bool params1 = false;
            bool params2 = false;
            bool hasMeshReferences = false;

            //bool size2frag3data3 = false;
            
            Tree = new List<SkeletonNode>();
            Meshes = new List<MeshReference>();

            var ba = new BitAnalyzer(flags);

            if (ba.IsBitSet(0))
                params1 = true;

            if (ba.IsBitSet(1))
                params2 = true;
            
            if (ba.IsBitSet(9))
                hasMeshReferences = true;

            //if (CheckBit(flags, 9))
            //size2frag3data3 = true;

            // number of track entries
            int size1 = reader.ReadInt32();

            //Log::get().write("Track entries: %i", size1);

            // optional 0x18 polygon animation - gives bogus references
            int fragment = reader.ReadInt32();

            // params - [0, 1, 2] - no idea what they are for
            if (params1)
            {
                reader.BaseStream.Position += (3 * sizeof(int));
            }

            if (params2)
            {
                BoundingRadius = reader.ReadSingle();
            }

            Skeleton = new List<SkeletonPieceData>();
            SkeletonPieceDictionary = new Dictionary<string, SkeletonPieceData>();

            // entries - bones
            for (int i = 0; i < size1; ++i)
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

                piece.AnimationTracks["default"] = fragments[entryFrag - 1] as TrackFragment;
                //piece.AnimationTracks["default"] = true;

                int entryFrag2 = reader.ReadInt32();

                // Reference to a 0x2D
                if (entryFrag2 >= 1 && entryFrag2 <= fragments.Count)
                {
                    pieceNew.Mesh = fragments[entryFrag2 - 1] as MeshReference;
                }

                int entrySize = reader.ReadInt32();

                List<int> moose = new List<int>();
                pieceNew.Children = new List<int>();

                for (int j = 0; j < entrySize; ++j)
                {
                    int pieceIndex = reader.ReadInt32();
                    moose.Add(pieceIndex);
                    pieceNew.Children.Add(pieceIndex);
                }
                
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
    }
}