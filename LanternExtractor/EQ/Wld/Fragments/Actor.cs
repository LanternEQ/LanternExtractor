using System.Collections.Generic;
using System.IO;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    /// <summary>
    /// Actor (0x14)
    /// Internal name: ACTORDEF
    /// Information about an actor that can be spawned into the world.
    /// Actors can be either static or animated.
    /// </summary>
    class Actor : WldFragment
    {
        /// <summary>
        /// Mesh reference (optional)
        /// </summary>
        public MeshReference MeshReference { get; private set; }
        
        /// <summary>
        /// Skeleton track reference (optional)
        /// </summary>
        public SkeletonHierarchyReference SkeletonReference { get; private set; }

        /// <summary>
        /// Camera reference (optional)
        /// </summary>
        public CameraReference CameraReference { get; private set; }
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);
            
            var reader = new BinaryReader(new MemoryStream(data));
                                     
            Name = stringHash[-reader.ReadInt32()];

            int flags = reader.ReadInt32();

            BitAnalyzer ba = new BitAnalyzer(flags);
            
            bool params1Exist = ba.IsBitSet(0);
            bool params2Exist = ba.IsBitSet(1);
            bool fragment2MustContainZero = ba.IsBitSet(7);
            
            // Is an index in the string hash
            int fragment1 = reader.ReadInt32();

            // For objects, SPRITECALLBACK - and it's the same reference value
            string stringValue = stringHash[-fragment1];
            
            // 1 for both static and animated objects
            int size1 = reader.ReadInt32();

            // The number of components (meshes, skeletons, camera references) the actor has
            // In all Trilogy files, there is only ever 1
            int componentCount = reader.ReadInt32();

            // 0 for both static and animated objects
            int fragment2 = reader.ReadInt32();
            
            if (params1Exist)
            {
                int params1 = reader.ReadInt32();
            }

            if (params2Exist)
            {
                reader.BaseStream.Position += 7 * sizeof(int);
            }
            
            // Size 1 entries
            for (int i = 0; i < size1; ++i)
            {
                // Always 1
                int dataPairCount = reader.ReadInt32();

                // Unknown purpose
                // Always 0 and 1.00000002E+30 
                for (int j = 0; j < dataPairCount; ++j)
                {
                    int value = reader.ReadInt32();
                    int value2 = reader.ReadInt16();
                    int value3 = reader.ReadInt16();
                }
            }

            if (componentCount > 1)
            {
                logger.LogWarning("Actor: More than one component references");
            }
            
            // Can contain either a skeleton reference (animated), mesh reference (static) or a camera reference
            for (int i = 0; i < componentCount; ++i)
            {
                int fragmentIndex = reader.ReadInt32();
                
                SkeletonReference = fragments[fragmentIndex - 1] as SkeletonHierarchyReference;

                if (SkeletonReference != null)
                {
                    break;
                }

                MeshReference = fragments[fragmentIndex - 1] as MeshReference;

                if (MeshReference != null)
                {
                    break;
                }
                
                // This only exists in the main zone WLD
                CameraReference = fragments[fragmentIndex - 1] as CameraReference;

                if (CameraReference != null)
                {
                    break;
                }

                logger.LogError($"Actor: Cannot link fragment with index {fragmentIndex}");
            }

            // Always 0 in qeynos2 objects
            int name3Bytes = reader.ReadInt32();
        }

        public override void OutputInfo(ILogger logger)
        {
            base.OutputInfo(logger);

            if (SkeletonReference == null && MeshReference == null)
            {
                return;
            }

            logger.LogInfo("-----");
        }
    }
}