using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x10 - Skeleton Track
    /// Describes the layout of a complete skeleton and which pieces connect to eachother
    /// </summary>
    class SkeletonTrack : WldFragment
    {
        public List<SkeletonPieceData> Skeleton { get; private set; }

        public Dictionary<string, SkeletonPieceData> SkeletonPieceDictionary { get; private set; }

        public override void Initialize(int index, int id, int size, byte[] data,
            Dictionary<int, WldFragment> fragments,
            Dictionary<int, string> stringHash, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            bool params1 = false;
            bool params2 = false;

            bool size2frag3data3 = false;

            var ba = new BitAnalyzer(flags);

            if (ba.IsBitSet(0))
                params1 = true;

            if (ba.IsBitSet(1))
                params2 = true;

            //if (CheckBit(flags, 9))
            //size2frag3data3 = true;

            // number of track entries
            int size1 = reader.ReadInt32();

            //Log::get().write("Track entries: %i", size1);

            // optional 0x18 polygon animation - gives bogus references
            int fragment = reader.ReadInt32();

            // params - [0, 1, 2] - no idea what they are for
            if (params1)
                reader.BaseStream.Position += (3 * sizeof(int));

            if (params2)
                reader.BaseStream.Position += (sizeof(float));

            Skeleton = new List<SkeletonPieceData>();
            SkeletonPieceDictionary = new Dictionary<string, SkeletonPieceData>();

            // entries - bones
            for (int i = 0; i < size1; ++i)
            {
                var piece = new SkeletonPieceData();

                // Create the skeleton structure
                // refers to this or another 0x10 fragment - confusing
                int entryNameRef = reader.ReadInt32();

                piece.Name = stringHash[-entryNameRef];

                // usually 0
                int entryFlags = reader.ReadInt32();

                // reference to an 0x13
                int entryFrag = reader.ReadInt32();

                piece.AnimationTracks = new Dictionary<string, SkeletonPieceTrackReference>();

                piece.AnimationTracks["default"] = fragments[entryFrag - 1] as SkeletonPieceTrackReference;
                piece.AnimationTracks["default"].Assigned = true;

                int entryFrag2 = reader.ReadInt32();

                // Reference to a 0x2D
                if (entryFrag2 != 0)
                {
                }

                int entrySize = reader.ReadInt32();

                List<int> moose = new List<int>();

                for (int j = 0; j < entrySize; ++j)
                {
                    int pieceIndex = reader.ReadInt32();
                    moose.Add(pieceIndex);
                }


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
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);
            logger.LogInfo("-----");
            logger.LogInfo("0x10: Skeleton pieces: " + Skeleton.Count);
        }

        public void AddNewTrack(SkeletonPieceTrackReference newTrack)
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