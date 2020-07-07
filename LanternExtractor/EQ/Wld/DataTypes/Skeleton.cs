using System.Collections.Generic;
using GlmSharp;
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

    /// <summary>
    /// A node in the skeleton tree
    /// </summary>
    public class SkeletonNode
    {
        public int Index;
        public string Name;

        public string FullPath;

        // Track fragment (TrackFragment) 0x13
        public TrackFragment Track;
        
        // MeshFragment (MeshFragment) 0x2D
        public MeshReference MeshReference;

        // The children indices in the tree
        public List<int> Children;
    }

    public class BoneTransform
    {
        // translation
        public vec3 Translation;

        // rotation
        public quat Rotation;
        public vec4 Rotation2;
        public vec3 Rotation3;

        public float Scale;

        public float padding;


        public vec3 Map(vec3 v)
        {
            return RotatedVec(v, Rotation2) + Translation;
        }

        public vec4 Map(vec4 v)
        {
            vec3 v2 = Map(v.xyz); //Map(v.asVec3());
            return new vec4(v2.x, v2.y, v2.z, 1f);
        }

        public BoneTransform Map(BoneTransform t)
        {
            BoneTransform newT = new BoneTransform();
            newT.Translation = Map(t.Translation);
            newT.Rotation2 = Rotation2 * t.Rotation2; //vec4::multiply(rotation, t.rotation);
            return newT;
        }

        vec3 RotatedVec(vec3 v, vec4 v2)
        {
            quat q = GetQuatFromVec4(v2);
            dvec3 res = q.Rotated(0, new vec3(v.x, v.y, v.z)).EulerAngles; // not sure about this line
            return new vec3((float) res.x, (float) res.y, (float) res.z);
        }

        quat GetQuatFromVec4(vec4 v)
        {
            return new quat(v.w, v.x, v.y, v.z);
        }
    }
}
