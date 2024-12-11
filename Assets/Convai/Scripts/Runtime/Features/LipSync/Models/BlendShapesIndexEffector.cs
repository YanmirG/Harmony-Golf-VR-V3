using System;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features.LipSync.Models
{
    [Serializable]
    public class BlendShapesIndexEffector
    {
        [SerializeField] public int index;

        [SerializeField] public float effectPercentage;
    }
}