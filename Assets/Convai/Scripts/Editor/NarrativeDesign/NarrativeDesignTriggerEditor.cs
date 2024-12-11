using System.Threading.Tasks;
using Convai.Scripts.Runtime.Features;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.NarrativeDesign
{
    [CustomEditor(typeof(NarrativeDesignTrigger))]
    public class NarrativeDesignTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            NarrativeDesignTrigger narrativeDesignTrigger = (NarrativeDesignTrigger)target;

            if (GUILayout.Button("Update Triggers"))
                if (narrativeDesignTrigger.convaiNPC != null)
                {
                    NarrativeDesignManager manager = narrativeDesignTrigger.convaiNPC.GetComponent<NarrativeDesignManager>();
                    if (manager != null)
                        manager.UpdateTriggerListAsync().ContinueWith(_ =>
                        {
                            narrativeDesignTrigger.UpdateAvailableTriggers();
                            EditorUtility.SetDirty(narrativeDesignTrigger);
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                }

            GUILayout.Space(10);
            DrawDefaultInspector();
            if (narrativeDesignTrigger.availableTriggers is { Count: > 0 })
                narrativeDesignTrigger.selectedTriggerIndex =
                    EditorGUILayout.Popup("Trigger", narrativeDesignTrigger.selectedTriggerIndex, narrativeDesignTrigger.availableTriggers.ToArray());
        }
    }
}