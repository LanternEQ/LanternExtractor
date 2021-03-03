using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Fragments
{
    public enum ActorType
    {
        Camera,
        Static,
        Skeletal,
        Particle,
        Sprite
    }

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
        /// Skeleton track reference (optional)
        /// </summary>
        public SkeletonHierarchy SecondSkeleton { get; private set; }
        
        /// <summary>
        /// Camera reference (optional)
        /// </summary>
        public CameraReference CameraReference { get; private set; }
        public ParticleSpriteReference ParticleSpriteReference { get; private set; }

        public Fragment07 Frag07;

        public ActorType ActorType;
        public string ReferenceName;
        
        public override void Initialize(int index, FragmentType id, int size, byte[] data,
            List<WldFragment> fragments,
            Dictionary<int, string> stringHash, bool isNewWldFormat, ILogger logger)
        {
            base.Initialize(index, id, size, data, fragments, stringHash, isNewWldFormat, logger);

            var reader = new BinaryReader(new MemoryStream(data));

            Name = stringHash[-reader.ReadInt32()];
            
            //logger.LogError($"Actor: {Name} - {size}");

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
                    SkeletonReference.SkeletonHierarchy.IsAssigned = true;

                    if (SkeletonReference.SkeletonHierarchy.Name.ToLower().Contains("146"))
                    {
                        
                    }
                    
                    break;
                }

                MeshReference = fragments[fragmentIndex - 1] as MeshReference;

                if (MeshReference != null)
                {
                    if (MeshReference.Mesh != null)
                    {
                        if (MeshReference.Mesh.Name.ToLower().Contains("146"))
                        {
                        
                        }
                    }

                    
                    break;
                }
                
                // This only exists in the main zone WLD
                CameraReference = fragments[fragmentIndex - 1] as CameraReference;

                if (CameraReference != null)
                {
                    break;
                }
                
                ParticleSpriteReference = fragments[fragmentIndex - 1] as ParticleSpriteReference;

                if (ParticleSpriteReference != null)
                {
                    break;
                }
                
                Frag07 = fragments[fragmentIndex - 1] as Fragment07;

                if (Frag07 != null)
                {
                    break;
                }
                
                logger.LogError($"Actor: Cannot link fragment with index {fragmentIndex}");
            }

            // Always 0 in qeynos2 objects
            int name3Bytes = reader.ReadInt32();
            
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                
            }
            
            CalculateActorType();

            if (Name.ToLower() == ("it2_actordef"))
            {
                
            }
        }

        private void CalculateActorType()
        {
            if (CameraReference != null)
            {
                ActorType = ActorType.Camera;
                ReferenceName = CameraReference.Name;
            }
            else if (SkeletonReference != null)
            {
                ActorType = ActorType.Skeletal;
            }
            else if (MeshReference != null)
            {
                // If the MeshReference is null, both the SkeletonReference and the SecondSkeleton are null
                ActorType = ActorType.Static;

                if (MeshReference != null)
                {
                    ReferenceName = MeshReference.Name;
                }
            }
            else if (ParticleSpriteReference != null)
            {
                ActorType = ActorType.Particle;
                ReferenceName = ParticleSpriteReference.Name;
            }
            else if (Frag07 != null)
            {
                ActorType = ActorType.Sprite;
                ReferenceName = Frag07.Name;
            }
            else
            {
                throw new System.NotImplementedException();
            }
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

        public void AssignSkeletonReference(SkeletonHierarchy skeleton)
        {
            SkeletonReference = new SkeletonHierarchyReference
            {
                SkeletonHierarchy = skeleton
            };
            CalculateActorType();
            skeleton.IsAssigned = true;
        }

        public Mesh GetMainMesh()
        {
            if (MeshReference != null)
            {
                return MeshReference.Mesh;
            }

            if (SkeletonReference != null)
            {
                return SkeletonReference.SkeletonHierarchy.Meshes.FirstOrDefault();
            }

            if (SecondSkeleton != null)
            {
                return SecondSkeleton.Meshes.FirstOrDefault();
            }

            return null;
        }
    }
}