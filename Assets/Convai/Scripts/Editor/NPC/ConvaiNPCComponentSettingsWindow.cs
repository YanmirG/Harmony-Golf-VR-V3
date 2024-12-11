using System.IO;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.Features.LongTermMemory;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ConvaiLipSync = Convai.Scripts.Runtime.Features.LipSync.ConvaiLipSync;

namespace Convai.Scripts.Editor.NPC
{
    /// <summary>
    ///     Editor window for managing Convai NPC components.
    /// </summary>
    public class ConvaiNPCComponentSettingsWindow : EditorWindow
    {
        private ConvaiNPC _convaiNPC;

        /// <summary>
        ///     Handles GUI events for the window.
        /// </summary>
        private void OnGUI()
        {
            titleContent = new GUIContent("Convai NPC Components");
            Vector2 windowSize = new(300, 250);
            minSize = windowSize;
            maxSize = windowSize;
            if (_convaiNPC == null)
            {
                EditorGUILayout.LabelField("No ConvaiNPC selected");
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUIUtility.labelWidth = 200f; // Set a custom label width

            _convaiNPC.IncludeActionsHandler = EditorGUILayout.Toggle(new GUIContent("NPC Actions", "Decides if Actions Handler is included"), _convaiNPC.IncludeActionsHandler);
            _convaiNPC.LipSync = EditorGUILayout.Toggle(new GUIContent("Lip Sync", "Decides if Lip Sync is enabled"), _convaiNPC.LipSync);
            _convaiNPC.HeadEyeTracking = EditorGUILayout.Toggle(new GUIContent("Head & Eye Tracking", "Decides if Head & Eye tracking is enabled"), _convaiNPC.HeadEyeTracking);
            _convaiNPC.EyeBlinking = EditorGUILayout.Toggle(new GUIContent("Eye Blinking", "Decides if Eye Blinking is enabled"), _convaiNPC.EyeBlinking);
            _convaiNPC.NarrativeDesignManager = EditorGUILayout.Toggle(new GUIContent("Narrative Design Manager", "Decides if Narrative Design Manager is enabled"),
                _convaiNPC.NarrativeDesignManager);
            _convaiNPC.ConvaiGroupNPCController =
                EditorGUILayout.Toggle(new GUIContent("Group NPC Controller", "Decides if this NPC can be a part of Convai NPC to NPC Conversation"),
                    _convaiNPC.ConvaiGroupNPCController);
            _convaiNPC.LongTermMemoryController =
                EditorGUILayout.Toggle(new GUIContent("Long Term Memory", "Component to toggle Long term memory for this character"),
                    _convaiNPC.LongTermMemoryController);
            _convaiNPC.NarrativeDesignKeyController =
                EditorGUILayout.Toggle(new GUIContent("Narrative Design Keys", "Adds handler for Narrative Design Keys for this character"),
                    _convaiNPC.NarrativeDesignKeyController);
            _convaiNPC.DynamicInfoController =
                EditorGUILayout.Toggle(new GUIContent("Dynamic Info", "Component used to send dynamic info like your game states to the character"),
                    _convaiNPC.DynamicInfoController);

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            if (GUILayout.Button("Apply Changes", GUILayout.Height(40)))
            {
                ApplyChanges();
                EditorUtility.SetDirty(_convaiNPC);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Close();
            }
        }

        /// <summary>
        ///     Refreshes the component states when the window gains focus.
        /// </summary>
        private void OnFocus()
        {
            RefreshComponentStates();
        }

        /// <summary>
        ///     Opens the Convai NPC Component Settings window.
        /// </summary>
        /// <param name="convaiNPC">The Convai NPC to manage.</param>
        public static void Open(ConvaiNPC convaiNPC)
        {
            ConvaiNPCComponentSettingsWindow window = GetWindow<ConvaiNPCComponentSettingsWindow>();
            window.titleContent = new GUIContent("Convai NPC Component Settings");
            window._convaiNPC = convaiNPC;
            window.RefreshComponentStates();
            window.Show();
        }

        /// <summary>
        ///     Refreshes the states of the components.
        /// </summary>
        private void RefreshComponentStates()
        {
            if (_convaiNPC != null)
            {
                _convaiNPC.IncludeActionsHandler = _convaiNPC.GetComponent<ConvaiActionsHandler>() is not null;
                _convaiNPC.LipSync = _convaiNPC.GetComponent<ConvaiLipSync>() is not null;
                _convaiNPC.HeadEyeTracking = _convaiNPC.GetComponent<ConvaiHeadTracking>() is not null;
                _convaiNPC.EyeBlinking = _convaiNPC.GetComponent<ConvaiBlinkingHandler>() is not null;
                _convaiNPC.NarrativeDesignManager = _convaiNPC.GetComponent<NarrativeDesignManager>() is not null;
                _convaiNPC.ConvaiGroupNPCController = _convaiNPC.GetComponent<ConvaiGroupNPCController>() is not null;
                _convaiNPC.LongTermMemoryController = _convaiNPC.GetComponent<ConvaiLTMController>() is not null;
                _convaiNPC.NarrativeDesignKeyController =
                    _convaiNPC.GetComponent<NarrativeDesignKeyController>() is not null;
                _convaiNPC.DynamicInfoController = _convaiNPC.GetComponent<DynamicInfoController>() is not null;
                Repaint();
            }
        }

        /// <summary>
        ///     Applies changes based on the user's selection in the inspector.
        /// </summary>
        private void ApplyChanges()
        {
            if (EditorUtility.DisplayDialog("Confirm Apply Changes",
                    "Do you want to apply the following changes?", "Yes", "No"))
            {
                ApplyComponent<ConvaiActionsHandler>(_convaiNPC.IncludeActionsHandler);
                ApplyComponent<ConvaiLipSync>(_convaiNPC.LipSync);
                ApplyComponent<ConvaiHeadTracking>(_convaiNPC.HeadEyeTracking);
                ApplyComponent<ConvaiBlinkingHandler>(_convaiNPC.EyeBlinking);
                ApplyComponent<NarrativeDesignManager>(_convaiNPC.NarrativeDesignManager);
                ApplyComponent<ConvaiGroupNPCController>(_convaiNPC.ConvaiGroupNPCController);
                ApplyComponent<ConvaiLTMController>(_convaiNPC.LongTermMemoryController);
                ApplyComponent<NarrativeDesignKeyController>(_convaiNPC.NarrativeDesignKeyController);
                ApplyComponent<DynamicInfoController>(_convaiNPC.DynamicInfoController);
            }
        }

        /// <summary>
        ///     Applies or removes a component based on the specified condition.
        ///     If the component is to be removed, its state is saved. If it's added, its state is restored if previously saved.
        /// </summary>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <param name="includeComponent">Whether to include the component.</param>
        private void ApplyComponent<T>(bool includeComponent) where T : Component
        {
            T component = _convaiNPC.GetComponent<T>();

            string savedDataFileName = Path.Combine(StateSaver.ROOT_DIRECTORY, _convaiNPC.characterID,
                $"{SceneManager.GetActiveScene().name}_{_convaiNPC.characterID}_{nameof(T)}_State.data");

            if (includeComponent)
            {
                if (component == null)
                {
                    component = _convaiNPC.gameObject.AddComponentSafe<T>();

                    if (File.Exists(savedDataFileName))
                        component.RestoreStateFromFile(savedDataFileName);
                }
            }
            else if (component != null)
            {
                component.SaveStateToFile(savedDataFileName);
                DestroyImmediate(component);
            }
        }
    }
}