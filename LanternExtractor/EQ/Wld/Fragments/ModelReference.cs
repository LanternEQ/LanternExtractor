using System;
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
        public List<SkeletonHierarchyReference> SkeletonReferences { get; private set; }

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

            SkeletonReferences = new List<SkeletonHierarchyReference>();

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];
            
            int flags = reader.ReadInt32();

            BitAnalyzer ba = new BitAnalyzer(flags);

            // For most objects, false, false true
            // For UFO false, false, false
            bool params1Exist = ba.IsBitSet(0);
            bool params2Exist = ba.IsBitSet(1);
            bool fragment2MustContainZero = ba.IsBitSet(7);

            if (Name.ToLower().Contains("temple"))
            {
                
            }
            
            // Is an index in the string hash
            int fragment1 = reader.ReadInt32();

            // For objects, SPRITECALLBACK
            string stringValue = stringHash[-fragment1];
            
            // 1 for both static and animated objects
            int size1 = reader.ReadInt32();

            // 1 for both static and animated objects
            int size2 = reader.ReadInt32();

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
                int numOfDatapair = reader.ReadInt32();

                // Unsure what this is
                // Always 0 and 1.00000002E+30 
                for (int j = 0; j < numOfDatapair; ++j)
                {
                    int value = reader.ReadInt32();
                    float value2 = reader.ReadSingle();
                }
            }

            // references
            for (int i = 0; i < size2; ++i)
            {
                int fragmentIndex = reader.ReadInt32();
                var skeletonTrackReference = fragments[fragmentIndex - 1] as SkeletonHierarchyReference;

                if (skeletonTrackReference != null)
                {
                    SkeletonReferences.Add(skeletonTrackReference);
                    continue;
                }

                var meshReference = fragments[fragmentIndex - 1] as MeshReference;

                if (meshReference != null)
                {
                    _meshes.Add(meshReference);
                    continue;
                }
                
                // In main zone, PLAYER1 reference fragment 14?
                logger.LogError("Cannot link skeleton or mesh reference");
            }

            // Always 0 in qeynos2 objects
            int name3Bytes = reader.ReadInt32();
            
            if (name3Bytes != 0)
            {
                
            }

            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
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