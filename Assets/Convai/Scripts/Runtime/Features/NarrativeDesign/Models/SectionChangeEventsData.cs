using System;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     Data class for Section Change Events
    /// </summary>
    [Serializable]
    public class SectionChangeEventsData
    {
        [SerializeField] public string id;
        [SerializeField] public UnityEvent onSectionStart;
        [SerializeField] public UnityEvent onSectionEnd;
        private NarrativeDesignManager _manager;

        private string SectionName
        {
            get
            {
                if (_manager == null) return string.Empty;
                SectionData sectionData = _manager.sectionDataList.Find(s => s.sectionId == id);
                return sectionData?.sectionName ?? "Unknown Section";
            }
        }

        /// <summary>
        ///     Initialize the Section Change Events
        /// </summary>
        /// <param name="manager"> The Narrative Design Manager </param>
        public void Initialize(NarrativeDesignManager manager)
        {
            _manager = manager;

            onSectionStart.RemoveListener(LogSectionStart);
            onSectionStart.AddListener(LogSectionStart);

            onSectionEnd.RemoveListener(LogSectionEnd);
            onSectionEnd.AddListener(LogSectionEnd);
        }

        private void LogSectionStart()
        {
            ConvaiLogger.DebugLog($"Section {SectionName} started", ConvaiLogger.LogCategory.Character);
        }

        private void LogSectionEnd()
        {
            ConvaiLogger.DebugLog($"Section {SectionName} ended", ConvaiLogger.LogCategory.Character);
        }
    }
}