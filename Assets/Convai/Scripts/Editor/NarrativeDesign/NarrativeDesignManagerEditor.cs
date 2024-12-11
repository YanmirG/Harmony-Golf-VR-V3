using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.Features;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.NarrativeDesign
{
    [CustomEditor(typeof(NarrativeDesignManager))]
    public class NarrativeDesignManagerEditor : UnityEditor.Editor
    {
        private NarrativeDesignManager _narrativeDesignManager;
        private SerializedProperty _sectionChangeEvents;
        private bool _sectionEventsExpanded = true;
        private Dictionary<string, bool> _sectionIdExpanded = new();
        private SerializedObject _serializedObject;

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(target);
            _narrativeDesignManager = (NarrativeDesignManager)target;
            FindProperties();
        }

        public override void OnInspectorGUI()
        {
            _serializedObject.Update();

            DrawUpdateButton();
            DrawSectionEvents();

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawUpdateButton()
        {
            if (GUILayout.Button("Check for Updates")) OnUpdateNarrativeDesignButtonClicked();
            GUILayout.Space(10);
        }

        private void DrawSectionEvents()
        {
            if (_narrativeDesignManager.sectionDataList.Count == 0) return;

            _sectionEventsExpanded = EditorGUILayout.Foldout(_sectionEventsExpanded, "Section Events", true, EditorStyles.foldoutHeader);
            if (!_sectionEventsExpanded) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < _narrativeDesignManager.sectionDataList.Count; i++) DrawSectionEvent(i);

            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                _narrativeDesignManager.OnSectionEventListChange();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSectionEvent(int index)
        {
            SectionData sectionData = _narrativeDesignManager.sectionDataList[index];
            string sectionId = sectionData.sectionId;

            EnsureSectionChangeEventsDataExists(sectionId);

            _sectionIdExpanded.TryAdd(sectionId, false);

            GUIStyle sectionIdStyle = CreateSectionIdStyle();

            string sectionIdText = $"{sectionData.sectionName} - {sectionId}";
            _sectionIdExpanded[sectionId] = EditorGUILayout.Foldout(_sectionIdExpanded[sectionId], sectionIdText, true, sectionIdStyle);

            if (_sectionIdExpanded[sectionId]) DrawSectionChangeEvents(index);
        }

        private void EnsureSectionChangeEventsDataExists(string sectionId)
        {
            if (!_narrativeDesignManager.sectionChangeEventsDataList.Exists(x => x.id == sectionId))
                _narrativeDesignManager.sectionChangeEventsDataList.Add(new SectionChangeEventsData { id = sectionId });
        }

        private GUIStyle CreateSectionIdStyle()
        {
            return new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
        }

        private void DrawSectionChangeEvents(int index)
        {
            EditorGUI.indentLevel++;
            SerializedProperty sectionChangeEventsProperty = _sectionChangeEvents.GetArrayElementAtIndex(index);
            EditorGUILayout.PropertyField(sectionChangeEventsProperty, GUIContent.none, true);
            EditorGUI.indentLevel--;
        }

        private async void OnUpdateNarrativeDesignButtonClicked()
        {
            await Task.WhenAll(_narrativeDesignManager.UpdateSectionListAsync(), _narrativeDesignManager.UpdateTriggerListAsync());

            // Remove section change events for deleted sections
            _narrativeDesignManager.sectionChangeEventsDataList = _narrativeDesignManager.sectionChangeEventsDataList
                .Where(changeEvent => _narrativeDesignManager.sectionDataList.Any(section => section.sectionId == changeEvent.id))
                .ToList();

            // Remove expanded states for deleted sections
            _sectionIdExpanded = _sectionIdExpanded
                .Where(kvp => _narrativeDesignManager.sectionDataList.Any(section => section.sectionId == kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            _serializedObject.Update(); // Update the serialized object
            _serializedObject.ApplyModifiedProperties();
            _narrativeDesignManager.OnSectionEventListChange();

            // Force the inspector to repaint
            Repaint();
        }


        private void FindProperties()
        {
            _serializedObject.FindProperty(nameof(NarrativeDesignManager.sectionDataList));
            _serializedObject.FindProperty(nameof(NarrativeDesignManager.triggerDataList));
            _sectionChangeEvents = _serializedObject.FindProperty(nameof(NarrativeDesignManager.sectionChangeEventsDataList));
        }
    }
}