using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GlmSharp;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.DataTypes
{

            /* Skeleton - Papa bear class
        Contains a Skeleton tree - a vector of Skeleton Nodes
        An animation reference *pose (could be the default animation)
        A map of animations <string, Animation*>
        Bounding radius

        Functions:
        Ability to add tracks, and copy animations from other skeletons.
             */
    public class Skeleton
    {
        public List<SkeletonNode> _tree;
        public Dictionary<int, string> _treePartMap = new Dictionary<int, string>();
        public float _boundingRadius;
        public Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();
        public Animation _pose;

        public Skeleton(List<SkeletonNode> skelDefTree, Dictionary<int, string> names, List<BoneTrack> tracks,
            float skelDefBoundingRadius)
        {
            _tree = skelDefTree;
            _treePartMap = names;
            _pose = new Animation("POS", tracks, this, this);
            _animations["POS"] = _pose;
            _boundingRadius = skelDefBoundingRadius;
            _animations[_pose.Name] = _pose;            
        }
        
        public void CopyAnimationsFrom(Skeleton skel)
        {
            foreach(var pair in skel._animations)
            {
                if (!_animations.ContainsKey(pair.Key))
                {
                    CopyFrom(skel, pair.Key);
                }
            }
        }
        
        public Animation CopyFrom(Skeleton skel, string animName)
        {
            if (skel == null || skel == this)
            {
                return null;
            }
            

            Animation anim = skel._animations[animName];
            if (anim == null)
            {
                return null;
            }
            Animation anim2 = _pose.Copy(animName, this);
            
            foreach(BoneTrack track in anim._boneTracks)
            {
                anim2.ReplaceTrack(track);
            }
            
            _animations[anim2.Name] = anim2;
            
            return anim2;
        }
    }
    
    public static class ListExtras
    {
        //    list: List<T> to resize
        //    size: desired new size
        // element: default value to insert

        public static void Resize<T>(this List<T> list, int size, T element = default(T))
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity)   // Optimization
                    list.Capacity = size;

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }
    }
    
    public class SkyModel
    {
        public string RootName;
        
        public HierSpriteDefFragment Skeleton;

        public Mesh Mesh;

        public int Frames;
    }

    /// <summary>
    /// A node in the skeleton tree
    /// </summary>
    public class SkeletonNode
    {
        public int Index;
        public string Name;

        public string FullPath;
        public string FullIndexPath;
        
        public int Flags;
        
        // Track fragment (TrackFragment) 0x13
        public TrackFragment Track;
        
        // MeshFragment (MeshFragment) 0x2D
        public MeshReference Mesh;

        // The children indices in the tree
        public List<int> Children;
    }

    public class Animation
    {
        public string Name;
        
        // Bone track set
        public List<BoneTrack> _boneTracks;
        
        public Skeleton _skeleton;

        public int _frameCount;

        public Animation(string name, List<BoneTrack> tracks, Skeleton skeleton, Object parent)
        {
            Name = name;
            _boneTracks = tracks;
            _skeleton = skeleton;
            _frameCount = 0;
            foreach (BoneTrack track in tracks)
            {
                _frameCount = Math.Max(_frameCount, (int)track._frameCount);
            }
        }
        
        public void TransformPiece(List<BoneTransform> bones, List<SkeletonNode> tree, int pieceId, double f, BoneTransform parentTransform)
        {
            SkeletonNode piece = tree[pieceId];
            BoneTrack track = _boneTracks[pieceId];
            BoneTransform pieceTrans = track.Interpolate(f);
            BoneTransform globalTrans = parentTransform.Map(pieceTrans);
            bones[pieceId] = globalTrans;
            foreach (int childId in piece.Children)
            {
                TransformPiece(bones, tree, childId, f, globalTrans);
            }
        }

        public Animation Copy(string newName, Skeleton parent)
        {
            return new Animation(newName, _boneTracks, _skeleton, parent);
        }
        
        public void ReplaceTrack(BoneTrack newTrack)
        {
            // strip animation name and character name from track name
            // TODO: Verify this
            string trackName = newTrack._name.Substring(6);
            
            // TODO: Verify this does something.
            for (var i = 0; i < _boneTracks.Count; i++)
            {
                BoneTrack track = _boneTracks[i];
                BoneTrack oldTrack = track;
                if (oldTrack._name.Substring(3) == trackName)
                {
                    _boneTracks[i] = newTrack;
                    _frameCount = Math.Max(_frameCount, newTrack._frameCount);
                    break;
                }
            }
        }
    }

    public class BoneTrack
    {
        public string _name;
        public int _frameCount;
        public List<BoneTransform> _frames; // is this an array?
        public TrackDefFragment _trackDef;

        public BoneTransform Interpolate(double f)
        {
            return _frames[0];

            // interpolates the frame and then between the frames
            /*int i = Math.Round(Math.Floor(f));
            BoneTransform result = BoneTransform::identity();
            if(frameCount > 0)
            {
                uint32_t index1 = (i % frameCount);
                uint32_t index2 = ((i + 1) % frameCount);
                const BoneTransform &trans1 = frames[index1];
                const BoneTransform &trans2 = frames[index2];
                result = BoneTransform::interpolate(trans1, trans2, (float)(f - i));
            }
            return result;*/
        }

    }

    public class BoneTransform
    {
        // translation
        public vec3 Translation;
        // rotation
        public quat Rotation;
        public vec4 Rotation2;
        public vec3 Rotation3;

        public float padding;

        
        public vec3 Map(vec3 v)
        {
            return RotatedVec(v, Rotation2) + Translation;
        }

        public vec4 Map(vec4 v)
        {
            vec3 v2 = Map(v.xyz);//Map(v.asVec3());
            return new vec4(v2.x, v2.y, v2.z, 1f);
        }

        public BoneTransform Map(BoneTransform t)
        {
            BoneTransform newT = new BoneTransform();
            newT.Translation = Map(t.Translation);
            newT.Rotation2 = Rotation2 * t.Rotation2;//vec4::multiply(rotation, t.rotation);
            return newT;
        }
        
        vec3 RotatedVec(vec3 v, vec4 v2)
        {
            quat q = GetQuatFromVec4(v2);
            dvec3 res = q.Rotated(0, new vec3(v.x, v.y, v.z)).EulerAngles; // not sure about this line
            return new vec3((float)res.x, (float)res.y, (float)res.z);
        }

        quat GetQuatFromVec4(vec4 v)
        {
            return new quat(v.w, v.x, v.y, v.z);
        }
    }
    


    public class AnimationArray
    {
        
    }

    public class CharacterModel
    {
        /// <summary>
        /// The (most commonly) 3 letter abbreviation for this character model
        /// </summary>
        public string ModelBase;
        
        /// <summary>
        /// The base from which this model copies the animations (often is the same as the modelbase)
        /// </summary>
        public string AnimationBase;
        
        /// <summary>
        /// The main mesh
        /// </summary>
        public WldMesh MainMesh;

        /// <summary>
        /// 
        /// </summary>
        public List<WldMesh> Meshes = new List<WldMesh>();
        
        /// <summary>
        /// The skeleton used by this character model
        /// </summary>
        public Skeleton Skeleton;

        /// <summary>
        /// The animations used by this model
        /// </summary>
        public Dictionary<string, Animation> Animations = new Dictionary<string, Animation>();
        
        /// <summary>
        /// Skin masks - need to flesh this out
        /// </summary>
        public List<int> SkinMasks = new List<int>();

        public Dictionary<string, Dictionary<int, Material>> NewSkins;

        public int DefaultSkinId;
        
        public CharacterModel(string actorName, WldMesh mainMesh)
        {
            ModelBase = actorName;
            MainMesh = mainMesh;
            NewSkins = new Dictionary<string, Dictionary<int, Material>>();
        }

        public void AddNewSkin(string slot, int mask, Material mat)
        {
            if (NewSkins.Count == 0)
            {
                DefaultSkinId = mask;
            }
            
            if (!NewSkins.ContainsKey(slot))
            {
                NewSkins[slot] = new Dictionary<int, Material>();
            }

            NewSkins[slot][mask] = mat;
        }
        
        public void AddPart(Mesh mesh, int skinId)
        {
            if (mesh == null)
            {
                return;
            }
            
            WldMesh meshPart = new WldMesh(mesh, Meshes.Count);
            
            AddPart(meshPart, skinId);
        }

        private void AddPart(WldMesh mesh, int skinId)
        {
            if (mesh == null)
            {
                return;
            }
            
            Meshes.Add(mesh);

            // Mark the part used by the skin.
            int skinMask = AddSkin(skinId, true);
            SetPartUsed(ref skinMask, mesh.PartId, true);
            SkinMasks[skinId] = skinMask;
        }

        public int AddSkin(int skinId, bool createSkin = false)
        {
            int newMask = (SkinMasks.Count > 0) ? SkinMasks[0] : 0;
            if(skinId >= SkinMasks.Count)
            {
                if(createSkin)
                {
                    ListExtras.Resize(SkinMasks, skinId + 1, newMask);
                }
                else
                {
                    return newMask;
                }
            }
            
            return SkinMasks[skinId];
        }

        void SetPartUsed(ref int skinMask, int partID, bool used)
        {
            if(used)
                skinMask |= (1 << partID);
            else
                skinMask &= ~(1 << partID);
        }

        public void SetAnimationBase(string gor)
        {
            AnimationBase = gor;
        }

        public void SetModelBase(string modelBase)
        {
            ModelBase = modelBase;
        }

        public bool IsPartUsed(int skinMask, int partId)
        {
            return Convert.ToBoolean(skinMask & (1 << partId));
        }

        public void ReplacePart(Mesh mesh, int skinId, int oldPartId)
        {
            if(mesh == null || (oldPartId >= Meshes.Count))
                return;
            
            AddPart(mesh, skinId);
            int skinMask = AddSkin(skinId, true);
            SetPartUsed(ref skinMask, oldPartId, false);
            SkinMasks[skinId] = skinMask;        
        }

        public void SetSkeleton(Skeleton skeleton1)
        {
            Skeleton = skeleton1;
        }

        public void AddAnimation(string name, Animation animation)
        {
            Animations[name] = animation;
        }
    }

    public class WldMesh
    {
        public WldMesh(Mesh meshDef, int partId)
        {
            PartId = partId;
            Def = meshDef;
            meshDef.Handled = true;
        }
        
        public Mesh Def;
        public int PartId;
        public WldMaterialPalette Palette;
        
        //public MeshData? not sure what this is!
        public WldMaterialPalette ImportPalette(PfsArchive archive)
        {
            Palette = new WldMaterialPalette(archive);
            Palette.SetDef(Def.MaterialList);
            Palette.CreateSlots();
            return Palette;
        }
    }

    public class WldMaterial
    {
        private Material _def;
        // material? _mat;
        private int _index;
        
        public void SetDef(Material matDef)
        {
            if(matDef.IsHandled)
                return;

            if ((_def != null) && (_def != matDef))
            {
                /*qDebug("warning: duplicated material definitions '%s' (fragments %d and %d)",
                    m_def->name().toLatin1().constData(), m_def->ID(), matDef->ID());*/
            }

            _def = matDef;
            //matDef.SetHandled(true);        
        }
    }
    
    public enum SlotID
    {
        eSlotHead = 0,
        eSlotChest = 1,
        eSlotArms = 2,
        eSlotBracer = 3,
        eSlotHands = 4,
        eSlotLegs = 5,
        eSlotFeet = 6,
        eSlotPrimary = 7,
        eSlotSeconday = 8,
        eSlotCount = 9
    };

    public class WldMaterialSlot
    {
        public string slotName;
        private SlotID slotId;
        private WldMaterial baseMat = new WldMaterial();

        private WldMaterial[] skinMats;
        private int offset;
        private bool visible;
        
        public WldMaterialSlot(string matName)
        {
            string charName;
            int skinID = 0;
            if(!WldMaterialPalette.ExplodeName(matName, out charName, out skinID, out slotName))
                slotName = matName.Replace("_MDF", "");
            offset = 0;
            visible = false;
            slotId = SlotID.eSlotCount;
            if(slotName.StartsWith("HE"))
                slotId = SlotID.eSlotHead;
            else if(slotName.StartsWith("CH"))
                slotId = SlotID.eSlotChest;
            else if(slotName.StartsWith("UA"))
                slotId = SlotID.eSlotArms;
            else if(slotName.StartsWith("FA"))
                slotId = SlotID.eSlotBracer;
            else if(slotName.StartsWith("HN"))
                slotId = SlotID.eSlotHands;
            else if(slotName.StartsWith("LG"))
                slotId = SlotID.eSlotLegs;
            else if(slotName.StartsWith("FT"))
                slotId = SlotID.eSlotFeet;
            
            skinMats = new WldMaterial[64];
        }

        public bool Visible { get; set; }

        public void AddSkinMaterial(int skinId, Material matDef)
        {
            if(skinId == 0)
            {
                // Skin zero is the base skin.
                baseMat.SetDef(matDef);
            }
            else
            {
                // TODO: not sure if this is correct
                skinMats[skinId-1] = new WldMaterial();
                skinMats[skinId-1].SetDef(matDef);
            }
        }
    }

    public class WldMaterialPalette
    {
        private MaterialList _def;
        private List<WldMaterialSlot> _materialSlots = new List<WldMaterialSlot>();

        public WldMaterialPalette(PfsArchive archive)
        {
            
        }
        
        public static bool ExplodeName(string defName, out string charName,
        out int skinId, out string partName)
        {
            // e.g. defName == 'ORCCH0201_MDF'
            // 'ORC' : character
            // 'CH' : piece (part 1)
            // '02' : palette ID
            // '01' : piece (part 2)
            Regex expression = new Regex("^\\w{4,5}\\d{4}_MDF$");
            
            if(expression.IsMatch(defName))
            {
                charName = defName.Substring(0,3);
                skinId = Convert.ToInt32(defName.Substring(5, 2));
                partName = defName.Substring(3, 2) + defName.Substring(7, 2);
                return true;
            }

            charName = string.Empty;
            skinId = 0;
            partName = string.Empty;

            return false;
        }
        
        public static bool ExplodeName2(string defName, out string charName,
            out int skinId, out string partName)
        {
            // e.g. defName == 'ORCCH0201_MDF'
            // 'ORC' : character
            // 'CH' : piece (part 1)
            // '02' : palette ID
            // '01' : piece (part 2)
            charName = string.Empty;
            skinId = 0;
            partName = string.Empty;  
            
            Regex expression = new Regex("^\\w{4,5}\\d{4}_MDF$");

            if (!expression.IsMatch(defName))
            {
                return false;
            }

            // piece
            string materialName = defName.Replace("_MDF", string.Empty);
            int currentIndex = materialName.Length;
            currentIndex -= 2;
            
            if (currentIndex < 0)
            {
                return false;
            }
            
            string piece2 = materialName.Substring(currentIndex, 2);
            currentIndex -= 2;
            
            if (currentIndex < 0)
            {
                return false;
            }
            
            string palette = materialName.Substring(currentIndex, 2);
            currentIndex -= 2;
            
            if (currentIndex < 0)
            {
                return false;
            }
            
            string piece1 = materialName.Substring(currentIndex, 2);
            string character = materialName.Substring(0, currentIndex);

            charName = character;
            skinId = Convert.ToInt32(palette);
            partName = piece1 + piece2;

            return true;
        }
        
        public void AddMeshMaterials(Mesh meshDef, int skinId)
        {
            var uvs = meshDef.RenderGroups;
            for(int i = 0; i < uvs.Count; i++)
            {
                int slotId = uvs[i].TextureIndex;
                WldMaterialSlot slot = _materialSlots[slotId];
                slot.AddSkinMaterial(skinId, meshDef.MaterialList.Materials[slotId]);
            }
        }

        public void SetDef(MaterialList materialList)
        {
            _def = materialList;
        }

        public void CreateSlots(bool addMatDefs = true)
        {
            if(_def == null)
                return;
            for(int i = 0; i < _def.Materials.Count; i++)
            {
                Material material = _def.Materials[i];
                WldMaterialSlot slot = new WldMaterialSlot(material.Name);
                if(addMatDefs)
                    slot.AddSkinMaterial(0, material);
                slot.Visible = material.ShaderType != ShaderType.Invisible;
                _materialSlots.Add(slot);
            }        
        }

        public WldMaterialSlot SlotByName(string name)
        {
            for(int i = 0; i < _materialSlots.Count; i++)
            {
                WldMaterialSlot slot = _materialSlots[i];
                if(slot.slotName == name)
                    return slot;
            }
            return null;
        }
    }
    
    public enum GenderId
    {
        eGenderMale = 0,
        eGenderFemale = 1,
        eGenderNeutral = 2
    };

    public enum RaceId
    {
        Human = 0,
    }
    
    public enum CharacterAnimation
    {
        eAnimInvalid = 0,
        eAnimBored,
        eAnimIdle,
        eAnimSit,
        eAnimRotate,
        eAnimRotate2,
        eAnimLoot,
        eAnimSwim,
        eAnimWalk,
        eAnimRun,
        eAnimSprint,
        eAnimJump,
        eAnimFall,
        eAnimCrouch,
        eAnimClimb,
        eAnimLoot2,
        eAnimSwim2,
        eAnimKick,
        eAnimPierce,
        eAnim2Handed1, // 3?
        eAnim2Handed2,
        eAnimAttackPrimary, // 8?
        eAnimAttackSecondary,
        eAnimAttack3,
        eAnimAttack4,
        eAnimArchery,
        eAnimAttackUnderwater,
        eAnimFlyingKick,
        eAnimDamageLow,
        eAnimDamageMed,
        eAnimDamageHigh,
        eAnimDeath,
        eAnimPlayString,
        eAnimPlayWind,
        eAnimSpell1,
        eAnimSpell2,
        eAnimSpell3,
        eAnimSpecialKick,
        eAnimSpecialPunch,
        eAnimSpecialBlow,
        eAnimEmoteCheer, // 27
        eAnimEmoteCry,   // 28
        eAnimEmoteWave,  // 29
        eAnimEmoteRude,  // 30
        // eAnimYawn 31
        // eAnimNod 48
        // eAnimApplaud 51
        // eAnimChuckle 54
        // eAnimWhistle 61
        // eAnimLaugh 63
        // eAnimTap 69
        // eAnimBow 70
        // eAnimSmile 77
        eAnimCount
    };
}