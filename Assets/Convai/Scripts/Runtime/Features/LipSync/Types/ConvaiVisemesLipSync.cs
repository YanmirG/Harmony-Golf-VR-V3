using System.Collections.Generic;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Extensions;
using Convai.Scripts.Runtime.Features.LipSync.Models;
using Convai.Scripts.Runtime.Features.LipSync.Visemes;
using Convai.Scripts.Runtime.LoggerSystem;
using Service;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features.LipSync.Types
{
    public class ConvaiVisemesLipSync : ConvaiLipSyncApplicationBase
    {
        private const float FRAMERATE = 1f / 100.0f;
        private readonly Viseme _defaultViseme = new();
        private Viseme _currentViseme;
        private Queue<Queue<VisemesData>> _visemesDataQueue = new();

        private void LateUpdate()
        {
            // Check if the dequeued frame is not null.
            if (_currentViseme == null) return;
            // Check if the frame represents silence (-2 is a placeholder for silence).
            if (Mathf.Approximately(_currentViseme.Sil, -2)) return;


            UpdateJawBoneRotation(new Vector3(0.0f, 0.0f, -90.0f));
            UpdateTongueBoneRotation(new Vector3(0.0f, 0.0f, -5.0f));

            if (HasHeadSkinnedMeshRenderer)
                UpdateMeshRenderer(FacialExpressionData.Head);
            if (HasTeethSkinnedMeshRenderer)
                UpdateMeshRenderer(FacialExpressionData.Teeth);
            if (HasTongueSkinnedMeshRenderer)
                UpdateMeshRenderer(FacialExpressionData.Tongue);

            UpdateJawBoneRotation(new Vector3(0.0f, 0.0f, -90.0f - CalculateBoneEffect(FacialExpressionData.JawBoneEffector) * 30f));
            UpdateTongueBoneRotation(new Vector3(0.0f, 0.0f, CalculateBoneEffect(FacialExpressionData.TongueBoneEffector) * 80f - 5f));
        }

        public override void Initialize(ConvaiLipSync convaiLipSync, ConvaiNPC convaiNPC)
        {
            base.Initialize(convaiLipSync, convaiNPC);
            InvokeRepeating(nameof(UpdateBlendShape), 0, FRAMERATE);
        }

        public override void ClearQueue()
        {
            _visemesDataQueue = new Queue<Queue<VisemesData>>();
            _currentViseme = new Viseme();
        }

        public override void PurgeExcessBlendShapeFrames()
        {
            if (_visemesDataQueue.Count == 0) return;
            if (!CanPurge(_visemesDataQueue.Peek())) return;
            ConvaiLogger.Info($"Purging {_visemesDataQueue.Peek().Count} Frames", ConvaiLogger.LogCategory.LipSync);
            _visemesDataQueue.Dequeue();
        }

        public override void EnqueueQueue(Queue<VisemesData> visemesFrames)
        {
            _visemesDataQueue.Enqueue(visemesFrames);
        }

        public override void EnqueueFrame(VisemesData viseme)
        {
            if (_visemesDataQueue.Count == 0) EnqueueQueue(new Queue<VisemesData>());
            _visemesDataQueue.Peek().Enqueue(viseme);
        }


        protected void UpdateBlendShape()
        {
            if (_visemesDataQueue is not { Count: > 0 })
            {
                _currentViseme = _defaultViseme;
                return;
            }

            // Dequeue the next frame of visemes data from the faceDataList.
            if (_visemesDataQueue.Peek() == null || _visemesDataQueue.Peek().Count <= 0)
            {
                _visemesDataQueue.Dequeue();
                return;
            }

            if (!ConvaiNPC.IsCharacterTalking) return;

            _currentViseme = _visemesDataQueue.Peek().Dequeue().Visemes;
        }

        private float CalculateBoneEffect(VisemeBoneEffectorList boneEffectorList)
        {
            if (boneEffectorList is null) return 0;
            return (
                       boneEffectorList.sil * _currentViseme.Sil +
                       boneEffectorList.pp * _currentViseme.Pp +
                       boneEffectorList.ff * _currentViseme.Ff +
                       boneEffectorList.th * _currentViseme.Th +
                       boneEffectorList.dd * _currentViseme.Dd +
                       boneEffectorList.kk * _currentViseme.Kk +
                       boneEffectorList.ch * _currentViseme.Ch +
                       boneEffectorList.ss * _currentViseme.Ss +
                       boneEffectorList.nn * _currentViseme.Nn +
                       boneEffectorList.rr * _currentViseme.Rr +
                       boneEffectorList.aa * _currentViseme.Aa +
                       boneEffectorList.e * _currentViseme.E +
                       boneEffectorList.ih * _currentViseme.Ih +
                       boneEffectorList.oh * _currentViseme.Oh +
                       boneEffectorList.ou * _currentViseme.Ou
                   )
                   / boneEffectorList.Total;
        }

        private void UpdateMeshRenderer(SkinMeshRendererData data)
        {
            VisemeEffectorsList effectorsList = data.VisemeEffectorsList;
            SkinnedMeshRenderer skinnedMesh = data.Renderer;
            Vector2 bounds = data.WeightBounds;
            if (effectorsList == null) return;
            Dictionary<int, float> finalModifiedValuesDictionary = new();
            CalculateBlendShapeEffect(effectorsList.pp, ref finalModifiedValuesDictionary, _currentViseme.Pp);
            CalculateBlendShapeEffect(effectorsList.ff, ref finalModifiedValuesDictionary, _currentViseme.Ff);
            CalculateBlendShapeEffect(effectorsList.th, ref finalModifiedValuesDictionary, _currentViseme.Th);
            CalculateBlendShapeEffect(effectorsList.dd, ref finalModifiedValuesDictionary, _currentViseme.Dd);
            CalculateBlendShapeEffect(effectorsList.kk, ref finalModifiedValuesDictionary, _currentViseme.Kk);
            CalculateBlendShapeEffect(effectorsList.ch, ref finalModifiedValuesDictionary, _currentViseme.Ch);
            CalculateBlendShapeEffect(effectorsList.ss, ref finalModifiedValuesDictionary, _currentViseme.Ss);
            CalculateBlendShapeEffect(effectorsList.nn, ref finalModifiedValuesDictionary, _currentViseme.Nn);
            CalculateBlendShapeEffect(effectorsList.rr, ref finalModifiedValuesDictionary, _currentViseme.Rr);
            CalculateBlendShapeEffect(effectorsList.aa, ref finalModifiedValuesDictionary, _currentViseme.Aa);
            CalculateBlendShapeEffect(effectorsList.e, ref finalModifiedValuesDictionary, _currentViseme.E);
            CalculateBlendShapeEffect(effectorsList.ih, ref finalModifiedValuesDictionary, _currentViseme.Ih);
            CalculateBlendShapeEffect(effectorsList.oh, ref finalModifiedValuesDictionary, _currentViseme.Oh);
            CalculateBlendShapeEffect(effectorsList.ou, ref finalModifiedValuesDictionary, _currentViseme.Ou);
            foreach (KeyValuePair<int, float> keyValuePair in finalModifiedValuesDictionary)
                skinnedMesh.SetBlendShapeWeightInterpolate(keyValuePair.Key, keyValuePair.Value * bounds.y - bounds.x, WeightBlendingPower);
        }

        private static void CalculateBlendShapeEffect(List<BlendShapesIndexEffector> effectors, ref Dictionary<int, float> dictionary, float value)
        {
            foreach (BlendShapesIndexEffector blendShapesIndexEffector in effectors)
                if (dictionary.ContainsKey(blendShapesIndexEffector.index))
                    dictionary[blendShapesIndexEffector.index] += value * blendShapesIndexEffector.effectPercentage;
                else
                    dictionary[blendShapesIndexEffector.index] = value * blendShapesIndexEffector.effectPercentage;
        }
    }
}