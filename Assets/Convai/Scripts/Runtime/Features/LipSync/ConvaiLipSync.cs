using System;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Extensions;
using Convai.Scripts.Runtime.Features.LipSync.Models;
using Convai.Scripts.Runtime.Features.LipSync.Types;
using Service;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features.LipSync
{
    public class ConvaiLipSync : MonoBehaviour
    {
        [HideInInspector] public FaceModel faceModel = FaceModel.OvrModelName;

        [field: SerializeField]
        [field: Tooltip("Assign the skin renderers and its respective effectors, along with the bones used for Facial Expression")]
        public FacialExpressionData FacialExpressionData { get; private set; } = new();

        [field: SerializeField]
        [field: Range(0f, 1f)]
        [field: Tooltip("This decides how much blending will occur between two different blendshape frames")]
        public float WeightBlendingPower { get; private set; } = 0.5f;

        [SerializeField] private List<string> characterEmotions;

        private ConvaiNPC _convaiNPC;
        public ConvaiLipSyncApplicationBase ConvaiLipSyncApplicationBase { get; private set; }

        /// <summary>
        ///     This function will automatically set any of the unassigned skinned mesh renderers to appropriate values using regex
        ///     based functions.
        ///     Sets the references of the required variables
        ///     Sets wait for lipsync to true
        /// </summary>
        private void Start()
        {
            FindSkinMeshRenderer();
            _convaiNPC = GetComponent<ConvaiNPC>();
            ConvaiLipSyncApplicationBase = gameObject.GetOrAddComponent<ConvaiVisemesLipSync>();
            ConvaiLipSyncApplicationBase.Initialize(this, _convaiNPC);
            SetCharacterLipSyncing(true);
        }

        private void OnDisable()
        {
            StopLipSync();
        }


        private void OnApplicationQuit()
        {
            StopLipSync();
        }

        public event Action<bool> OnCharacterLipSyncing;

        private void FindSkinMeshRenderer()
        {
            if (FacialExpressionData.Head.Renderer == null)
                FacialExpressionData.Head.Renderer = transform.GetComponentOnChildWithMatchingRegex<SkinnedMeshRenderer>("(.*_Head|CC_Base_Body)");
            if (FacialExpressionData.Teeth.Renderer == null)
                FacialExpressionData.Teeth.Renderer = transform.GetComponentOnChildWithMatchingRegex<SkinnedMeshRenderer>("(.*_Teeth|CC_Base_Teeth)");
            if (FacialExpressionData.Tongue.Renderer == null)
                FacialExpressionData.Tongue.Renderer = transform.GetComponentOnChildWithMatchingRegex<SkinnedMeshRenderer>("(.*_Tongue|CC_Base_Tongue)");
        }

        /// <summary>
        ///     Overrides the character emotions list
        /// </summary>
        /// <param name="newEmotions">list of new emotions</param>
        public void SetCharacterEmotions(List<string> newEmotions)
        {
            characterEmotions = new List<string>(newEmotions);
        }

        /// <summary>
        ///     Returns Direct reference of the character emotions [Not Recommended to directly change this list]
        /// </summary>
        /// <returns></returns>
        public List<string> GetCharacterEmotions()
        {
            return characterEmotions;
        }


        /// <summary>
        ///     Fires an event with update the Character Lip Syncing State
        /// </summary>
        /// <param name="value"></param>
        private void SetCharacterLipSyncing(bool value)
        {
            OnCharacterLipSyncing?.Invoke(value);
        }

        /// <summary>
        ///     Purges the latest chuck of lipsync frames
        /// </summary>
        public void PurgeExcessFrames()
        {
            ConvaiLipSyncApplicationBase?.PurgeExcessBlendShapeFrames();
        }

        /// <summary>
        ///     Stops the Lipsync by clearing the frames queue
        /// </summary>
        public void StopLipSync()
        {
            ConvaiLipSyncApplicationBase?.ClearQueue();
        }
    }
}