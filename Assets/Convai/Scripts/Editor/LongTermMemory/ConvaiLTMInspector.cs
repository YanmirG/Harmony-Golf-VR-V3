using System;
using Convai.Scripts.Runtime.Features.LongTermMemory;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.LongTermMemory
{
    [CustomEditor(typeof(ConvaiLTMController))]
    public class ConvaiLTMInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ConvaiLTMController controller = (ConvaiLTMController)target;
            SetHeader(controller);
            AddButtons(controller);
        }

        private void AddButtons(ConvaiLTMController controller)
        {
            switch (controller.LTMStatus)
            {
                case LTMStatus.NotDefined:
                    return;
                case LTMStatus.Disabled:
                    EnableButton(controller);
                    break;
                case LTMStatus.Enabled:
                    DisableButton(controller);
                    break;
                case LTMStatus.Failed:
                    RetryButton(controller);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SetHeader(ConvaiLTMController controller)
        {
            GUIStyle style = new()
            {
                richText = true,
                fontStyle = FontStyle.Bold
            };
            switch (controller.LTMStatus)
            {
                case LTMStatus.NotDefined:
                    style.normal.textColor = Color.yellow;
                    GUILayout.Label("Long Term Memory <b>status is getting Updated</b>", style);
                    break;
                case LTMStatus.Enabled:
                    style.normal.textColor = Color.green;
                    GUILayout.Label("Long Term Memory <b>is Enabled</b>", style);
                    break;
                case LTMStatus.Disabled:
                    style.normal.textColor = Color.red;
                    GUILayout.Label("Long Term Memory <b>is Disabled</b>", style);
                    break;
                case LTMStatus.Failed:
                    style.normal.textColor = Color.red;
                    GUILayout.Label("Long Term Memory <b>could not be updated</b>", style);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RetryButton(ConvaiLTMController controller)
        {
            if (GUILayout.Button("Retry"))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                controller.GetLTMStatus();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private void DisableButton(ConvaiLTMController controller)
        {
            if (GUILayout.Button("Disable LTM")) DisableLTMTask(controller);
        }

        private void EnableButton(ConvaiLTMController controller)
        {
            if (GUILayout.Button("Enable LTM")) EnableLTMTask(controller);
        }

        private void EnableLTMTask(ConvaiLTMController controller)
        {
            controller.StartCoroutine(controller.ToggleLTM(true));
        }

        private void DisableLTMTask(ConvaiLTMController controller)
        {
            controller.StartCoroutine(controller.ToggleLTM(false));
        }
    }
}