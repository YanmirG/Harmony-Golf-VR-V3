using System;
using Convai.Scripts.Runtime.Features.LipSync.Visemes;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features.LipSync.Models
{
    [Serializable]
    public class SkinMeshRendererData
    {
        public SkinnedMeshRenderer Renderer;
        public VisemeEffectorsList VisemeEffectorsList;

        [Tooltip("Lower and Upper bound of the Blendshape weight, Ex: 0-1, or 0-100")]
        public Vector2 WeightBounds = new(0, 1);
    }
}