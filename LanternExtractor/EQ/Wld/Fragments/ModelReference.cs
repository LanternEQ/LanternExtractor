using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// 0x14 - Model Reference
    /// The starting point for all models
    /// </summary>
    class ModelReference : WldFragment
    {
        /// <summary>
        /// The skeleton track reference
        /// </summary>
        public List<HierSpriteFragment> SkeletonReferences { get; private set; }

        /// <summary>
        /// The mesh reference
        /// </summary>
        public Mesh Mesh { get; private set; }

        public List<MeshReference> _meshes = new List<MeshReference>();

        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];

            SkeletonReferences = new List<HierSpriteFragment>();

            int flags = reader.ReadInt32();

            int fragment1 = reader.ReadInt32();

            int size1 = reader.ReadInt32();

            int size2 = reader.ReadInt32();

            int fragment2 = reader.ReadInt32();

            var bitAnalyzer = new BitAnalyzer(flags);

            if (bitAnalyzer.IsBitSet(0))
            {
                int params1 = reader.ReadInt32();
            }

            if (bitAnalyzer.IsBitSet(1))
            {
                reader.BaseStream.Position += 7 * sizeof(int);
            }

            // size 1 entries
            for (int i = 0; i < size1; ++i)
            {
                int numOfDatapair = reader.ReadInt32();

                for (int j = 0; j < numOfDatapair; ++j)
                {
                    reader.ReadInt32();
                    reader.ReadSingle();
                }
            }

            // references
            for (int i = 0; i < size2; ++i)
            {
                int fragmentIndex = reader.ReadInt32();
                var skeletonTrackReference = fragments[fragmentIndex - 1] as HierSpriteFragment;

                if (skeletonTrackReference != null)
                {
                    SkeletonReferences.Add(skeletonTrackReference);
                    continue;
                }

                var meshReference = fragments[fragmentIndex - 1] as MeshReference;

                if (meshReference != null)
                {
                    _meshes.Add(meshReference);
                }
            }
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (SkeletonReferences == null && Mesh == null)
            {
                return;
            }

            logger.LogInfo("-----");

            if (SkeletonReferences != null)
            {
                logger.LogInfo("0x14: Skeleton reference count: " + SkeletonReferences.Count);
            }

            if (Mesh != null)
            {
                logger.LogInfo("0x14: Mesh reference: " + Mesh.Index);
            }
        }
    }
}