using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.LoggerSettings
{
    public class LoggerSettingsLogic
    {
        private readonly Runtime.LoggerSystem.LoggerSettings _loggerSettings = Resources.Load<Runtime.LoggerSystem.LoggerSettings>("LoggerSettings");
        private Dictionary<FieldInfo, Toggle> _selectAllMapping;
        private Dictionary<FieldInfo, List<Toggle>> _toggleMapping;

        public bool GetLogLevelEnabledStatus(FieldInfo fieldInfo, ConvaiLogger.LogLevel logLevel)
        {
            return ((ConvaiLogger.LogLevel)fieldInfo.GetValue(_loggerSettings) & logLevel) != 0;
        }

        public bool IsAllSelectedForCategory(FieldInfo fieldInfo)
        {
            ConvaiLogger.LogLevel logLevel = (ConvaiLogger.LogLevel)fieldInfo.GetValue(_loggerSettings);
            return Enum.GetValues(typeof(ConvaiLogger.LogLevel))
                .Cast<ConvaiLogger.LogLevel>()
                .Where(enumType => enumType != ConvaiLogger.LogLevel.None)
                .All(enumType => (logLevel & enumType) != 0);
        }

        public void AddToToggleDictionary(FieldInfo fieldInfo, Toggle toggle)
        {
            _toggleMapping ??= new Dictionary<FieldInfo, List<Toggle>>();
            if (_toggleMapping.ContainsKey(fieldInfo))
                _toggleMapping[fieldInfo].Add(toggle);
            else
                _toggleMapping.Add(fieldInfo, new List<Toggle> { toggle });
        }

        public void AddToSelectAllDictionary(FieldInfo fieldInfo, Toggle toggle)
        {
            _selectAllMapping ??= new Dictionary<FieldInfo, Toggle>();
            _selectAllMapping[fieldInfo] = toggle;
        }

        public void OnToggleClicked(FieldInfo fieldInfo, ConvaiLogger.LogLevel logLevel, bool status)
        {
            UpdateEnumFlag(fieldInfo, logLevel, status);
            _selectAllMapping[fieldInfo].SetValueWithoutNotify(IsAllSelectedForCategory(fieldInfo));
            EditorUtility.SetDirty(_loggerSettings);
        }

        private void UpdateEnumFlag(FieldInfo fieldInfo, ConvaiLogger.LogLevel logLevel, bool status)
        {
            ConvaiLogger.LogLevel value = (ConvaiLogger.LogLevel)fieldInfo.GetValue(_loggerSettings);
            switch (status)
            {
                case true:
                    value |= logLevel;
                    break;
                case false:
                    value &= ~logLevel;
                    break;
            }

            fieldInfo.SetValue(_loggerSettings, value);
        }

        public void OnSelectAllClicked(FieldInfo fieldInfo, bool status)
        {
            ConvaiLogger.LogLevel value = status ? (ConvaiLogger.LogLevel)31 : 0;
            fieldInfo.SetValue(_loggerSettings, value);
            EditorUtility.SetDirty(_loggerSettings);

            UpdateToggleValues(fieldInfo, status);
        }

        private void UpdateToggleValues(FieldInfo fieldInfo, bool status)
        {
            foreach (Toggle toggle in _toggleMapping[fieldInfo]) toggle.SetValueWithoutNotify(status);
        }

        public void ClearAllOnClicked()
        {
            foreach (FieldInfo fieldInfo in typeof(Runtime.LoggerSystem.LoggerSettings).GetFields())
            {
                foreach (ConvaiLogger.LogLevel enumType in Enum.GetValues(typeof(ConvaiLogger.LogLevel)).Cast<ConvaiLogger.LogLevel>()) UpdateEnumFlag(fieldInfo, enumType, false);
                UpdateToggleValues(fieldInfo, false);
                UpdateSelectAllValues(fieldInfo, false);
            }

            EditorUtility.SetDirty(_loggerSettings);
        }

        private void UpdateSelectAllValues(FieldInfo fieldInfo, bool status)
        {
            _selectAllMapping[fieldInfo].SetValueWithoutNotify(status);
        }

        public void SelectAllOnClicked()
        {
            foreach (FieldInfo fieldInfo in typeof(Runtime.LoggerSystem.LoggerSettings).GetFields())
            {
                foreach (ConvaiLogger.LogLevel enumType in Enum.GetValues(typeof(ConvaiLogger.LogLevel)).Cast<ConvaiLogger.LogLevel>()) UpdateEnumFlag(fieldInfo, enumType, true);
                UpdateToggleValues(fieldInfo, true);
                UpdateSelectAllValues(fieldInfo, true);
            }

            EditorUtility.SetDirty(_loggerSettings);
        }
    }
}