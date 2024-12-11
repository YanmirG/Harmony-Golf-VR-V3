using Convai.Scripts.Runtime.Attributes;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features.LipSync.Visemes
{
    [CreateAssetMenu(fileName = "Convai Viseme Bone Effector", menuName = "Convai/Expression/Visemes Bone Effector", order = 0)]
    public class VisemeBoneEffectorList : ScriptableObject
    {
        [SerializeField] public float sil;
        [SerializeField] public float pp;
        [SerializeField] public float ff;
        [SerializeField] public float th;
        [SerializeField] public float dd;
        [SerializeField] public float kk;
        [SerializeField] public float ch;
        [SerializeField] public float ss;
        [SerializeField] public float nn;
        [SerializeField] public float rr;
        [SerializeField] public float aa;
        [SerializeField] public float e;
        [SerializeField] public float ih;
        [SerializeField] public float oh;
        [SerializeField] public float ou;


        [field: SerializeField]
        [field: ReadOnly]
        public float Total { get; private set; }

        private void OnValidate()
        {
            Total = 0;
            Total += sil;
            Total += pp;
            Total += ff;
            Total += th;
            Total += dd;
            Total += kk;
            Total += ch;
            Total += ss;
            Total += nn;
            Total += rr;
            Total += aa;
            Total += e;
            Total += ih;
            Total += oh;
            Total += ou;
        }
    }
}