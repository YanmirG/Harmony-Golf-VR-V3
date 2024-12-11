using System;
using Convai.Scripts.Runtime.Features.LipSync.Visemes;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Convai.Scripts.Runtime.Features.LipSync.Models
{
    [Serializable]
    public class FacialExpressionData
    {
        [Tooltip("Assign the Skin Renderer and Effector for Head")]
        public SkinMeshRendererData Head;

        [Tooltip("Assign the Skin Renderer and Effector for Teeth")]
        public SkinMeshRendererData Teeth;

        [Tooltip("Assign the Skin Renderer and Effector for Tongue")]
        public SkinMeshRendererData Tongue;

        [Tooltip("Assign the Viseme Bone Effector List for Jaw")]
        public VisemeBoneEffectorList JawBoneEffector;

        [Tooltip("Assign the Viseme Bone Effector List for Tongue")]
        public VisemeBoneEffectorList TongueBoneEffector;

        [Tooltip("Assign the bone which effects movement of jaw")]
        public GameObject JawBone;

        [Tooltip("Assign the bone which effects movement of tongue")]
        public GameObject TongueBone;
    }
}