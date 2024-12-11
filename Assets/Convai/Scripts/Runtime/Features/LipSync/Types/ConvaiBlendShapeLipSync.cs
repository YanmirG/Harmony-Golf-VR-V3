using System.Collections.Generic;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Features.LipSync;
using Convai.Scripts.Runtime.LoggerSystem;
using Service;

/*
! This class is a Work in progress and can produce un expected results, Convai does not advise to use this class in production, please use this with extreme caution
*/

namespace Convai.Scripts.Runtime.Features
{
    public class ConvaiBlendShapeLipSync : ConvaiLipSyncApplicationBase
    {
        private const float A2XFRAMERATE = 1f / 30f;
        private Queue<Queue<ARKitBlendShapes>> _blendShapesQueue = new();
        private ARKitBlendShapes _currentBlendshape;

        // private void Update()
        // {
        //     if (_currentBlendshape == null) return;
        //     UpdateJawBoneRotation(new Vector3(0.0f, 0.0f, -90.0f - _currentBlendshape.JawOpen * 30f));
        //     UpdateTongueBoneRotation(new Vector3(0.0f, 0.0f, -5.0f * _currentBlendshape.TongueOut));
        //     if (!HasHeadSkinnedMeshRenderer) return;
        //     foreach (PropertyInfo propertyInfo in typeof(ARKitBlendShapes).GetProperties())
        //     {
        //         if (propertyInfo.PropertyType != typeof(float)) continue;
        //         string fieldName = propertyInfo.Name;
        //         float value = (float)propertyInfo.GetValue(_currentBlendshape);
        //         if (HeadMapping.TryGetValue(fieldName, out int index))
        //             HeadSkinMeshRenderer.SetBlendShapeWeightInterpolate(
        //                 index,
        //                 value * WeightMultiplier,
        //                 Time.deltaTime
        //             );
        //     }
        // }

        protected Dictionary<string, string> GetHeadRegexMapping()
        {
            #region Regex Finders

            string prefix = "(?:[A-Z]\\d{1,2}_)?";
            string spacer = "[\\s_]*";
            string mouth = "[Mm]outh";
            string nose = "[Nn]ose";
            string left = "[Ll]eft";
            string right = "[Rr]ight";
            string up = "[Uu]p";
            string down = "[Dd]own";
            string lower = "[Ll]ower";
            string upper = "[Uu]pper";
            string open = "[Oo]pen";
            string funnel = "[Ff]unnel";
            string pucker = "[Pp]ucker";
            string sneer = "[Ss]neer";
            string cheek = "[Cc]heek";
            string squint = "[Ss]quint";
            string brow = "[Bb]row";
            string outer = "[Oo]uter";
            string inner = "[Ii]nner";
            string eye = "[Ee]ye";
            string blink = "[Bb]link";
            string look = "[Ll]ook";
            string In = "[Ii]n";
            string Out = "[Oo]ut";
            string wide = "[Ww]ide";
            string forward = "[Ff]orward";
            string jaw = "[Jj]aw";
            string close = "[Cc]lose";
            string smile = "[Ss]mile";
            string frown = "[Ff]rown";
            string dimple = "[Dd]imple";
            string stretch = "[Ss]tretch";
            string roll = "[Rr]oll";
            string shrug = "[Ss]hrug";
            string press = "[Pp]ress";

            #endregion

            return new Dictionary<string, string>
            {
                { "TougueOut", $"{prefix}[Tt]ougue{spacer}[Oo]ut" },

                { "NoseSneerRight", $"{prefix}{nose}{spacer}{sneer}{spacer}{right}" },
                { "NoseSneerLeft", $"{prefix}{nose}{spacer}{sneer}{spacer}{left}" },

                { "CheekSquintRight", $"{prefix}{cheek}{spacer}{squint}{spacer}{right}" },
                { "CheekSquintLeft", $"{prefix}{cheek}{spacer}{squint}{spacer}{left}" },
                { "CheekPuff", $"{prefix}{cheek}{spacer}[Pp]uff" },

                { "BrowDownLeft", $"{prefix}{brow}{spacer}{down}{spacer}{left}" },
                { "BrowDownRight", $"{prefix}{brow}{spacer}{down}{spacer}{right}" },
                { "BrowInnerUp", $"{prefix}{brow}{spacer}{inner}{spacer}{up}" },
                { "BrowOuterUpLeft", $"{prefix}{brow}{spacer}{outer}{spacer}{up}{spacer}{left}" },
                { "BrowOuterUpRight", $"{prefix}{brow}{spacer}{outer}{spacer}{up}{spacer}{right}" },

                { "EyeBlinkLeft", $"{prefix}{eye}{spacer}{blink}{spacer}{left}" },
                { "EyeLookDownLeft", $"{prefix}{eye}{spacer}{look}{spacer}{In}{left}" },
                { "EyeLookInLeft", $"{prefix}{eye}{spacer}{look}{spacer}{In}{spacer}{left}" },
                { "EyeLookOutLeft", $"{prefix}{eye}{spacer}{look}{spacer}{Out}{spacer}{left}" },
                { "EyeLookUpLeft", $"{prefix}{eye}{spacer}{look}{spacer}{up}{spacer}{left}" },
                { "EyeSquintLeft", $"{prefix}{eye}{spacer}{squint}{spacer}{left}" },
                { "EyeWideLeft", $"{prefix}{eye}{spacer}{wide}{spacer}{left}" },

                { "EyeBlinkRight", $"{prefix}{eye}{spacer}{blink}{spacer}{right}" },
                { "EyeLookDownRight", $"{prefix}{eye}{spacer}{look}{spacer}{In}{right}" },
                { "EyeLookInRight", $"{prefix}{eye}{spacer}{look}{spacer}{In}{spacer}{right}" },
                { "EyeLookOutRight", $"{prefix}{eye}{spacer}{look}{spacer}{Out}{spacer}{right}" },
                { "EyeLookUpRight", $"{prefix}{eye}{spacer}{look}{spacer}{up}{spacer}{right}" },
                { "EyeSquintRight", $"{prefix}{eye}{spacer}{squint}{spacer}{right}" },
                { "EyeWideRight", $"{prefix}{eye}{spacer}{wide}{spacer}{right}" },

                { "JawForward", $"{prefix}{jaw}{spacer}{forward}" },
                { "JawLeft", $"{prefix}{jaw}{spacer}{left}" },
                { "JawRight", $"{prefix}{jaw}{spacer}{right}" },
                { "JawOpen", $"{prefix}{jaw}{spacer}{open}" },

                { "MouthClose", $"{prefix}{mouth}{spacer}{close}" },
                { "MouthFunnel", $"{prefix}{mouth}{spacer}{funnel}" },
                { "MouthPucker", $"{prefix}{mouth}{spacer}{pucker}" },

                { "Mouthleft", $"{prefix}{mouth}{spacer}{left}" },
                { "MouthRight", $"{prefix}{mouth}{spacer}{right}" },

                { "MouthSmileLeft", $"{prefix}{mouth}{spacer}{smile}{spacer}{left}" },
                { "MouthSmileRight", $"{prefix}{mouth}{spacer}{smile}{spacer}{right}" },

                { "MouthFrownLeft", $"{prefix}{mouth}{spacer}{frown}{spacer}{left}" },
                { "MouthFrownRight", $"{prefix}{mouth}{spacer}{frown}{spacer}{right}" },

                { "MouthDimpleLeft", $"{prefix}{mouth}{spacer}{dimple}{spacer}{left}" },
                { "MouthDimpleRight", $"{prefix}{mouth}{spacer}{dimple}{spacer}{right}" },

                { "MouthStretchLeft", $"{prefix}{mouth}{spacer}{stretch}{spacer}{left}" },
                { "MouthStretchRight", $"{prefix}{mouth}{spacer}{stretch}{spacer}{right}" },

                { "MouthRollLower", $"{prefix}{mouth}{spacer}{roll}{spacer}{lower}" },
                { "MouthRollUpper", $"{prefix}{mouth}{spacer}{roll}{spacer}{upper}" },

                { "MouthShrugLower", $"{prefix}{mouth}{spacer}{shrug}{spacer}{lower}" },
                { "MouthShrugUpper", $"{prefix}{mouth}{spacer}{shrug}{spacer}{upper}" },

                { "MouthPressLeft", $"{prefix}{mouth}{spacer}{press}{spacer}{left}" },
                { "MouthPressRight", $"{prefix}{mouth}{spacer}{press}{spacer}{right}" },

                { "MouthLowerDownLeft", $"{prefix}{mouth}{spacer}{lower}{spacer}{down}{spacer}{left}" },
                { "MouthLowerDownRight", $"{prefix}{mouth}{spacer}{lower}{spacer}{down}{spacer}{right}" },

                { "MouthUpperUpLeft", $"{prefix}{mouth}{spacer}{upper}{spacer}{up}{spacer}{left}" },
                { "MouthUpperUpRight", $"{prefix}{mouth}{spacer}{upper}{spacer}{up}{spacer}{right}" }
            };
        }

        public override void Initialize(ConvaiLipSync convaiLipSync, ConvaiNPC convaiNPC)
        {
            base.Initialize(convaiLipSync, convaiNPC);
            InvokeRepeating(nameof(UpdateBlendShape), 0, A2XFRAMERATE);
        }

        protected virtual void UpdateBlendShape()
        {
            if (_blendShapesQueue == null || _blendShapesQueue.Count <= 0)
            {
                _currentBlendshape = new ARKitBlendShapes();
                return;
            }

            if (_blendShapesQueue.Peek().Count == 0)
            {
                _blendShapesQueue.Dequeue();
                return;
            }

            if (!ConvaiNPC.IsCharacterTalking) return;
            _currentBlendshape = _blendShapesQueue.Peek().Dequeue();
        }

        public override void PurgeExcessBlendShapeFrames()
        {
            if (_blendShapesQueue.Count <= 0) return;
            if (!CanPurge(_blendShapesQueue.Peek())) return;
            ConvaiLogger.Info($"Purging {_blendShapesQueue.Peek().Count} frames", ConvaiLogger.LogCategory.LipSync);
            _blendShapesQueue.Dequeue();
        }

        public override void ClearQueue()
        {
            _blendShapesQueue = new Queue<Queue<ARKitBlendShapes>>();
            _currentBlendshape = new ARKitBlendShapes();
        }

        public override void EnqueueQueue(Queue<ARKitBlendShapes> blendshapeFrames)
        {
            _blendShapesQueue.Enqueue(blendshapeFrames);
        }

        public override void EnqueueFrame(ARKitBlendShapes blendshapeFrame)
        {
            if (_blendShapesQueue.Count == 0) EnqueueQueue(new Queue<ARKitBlendShapes>());
            _blendShapesQueue.Peek().Enqueue(blendshapeFrame);
        }
    }
}