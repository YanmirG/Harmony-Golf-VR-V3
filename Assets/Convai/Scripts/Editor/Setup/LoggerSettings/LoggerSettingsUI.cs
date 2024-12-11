using System;
using System.Reflection;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.LoggerSettings
{
    public class LoggerSettingsUI
    {
        private readonly LoggerSettingsLogic _loggerSettingsLogic;

        public LoggerSettingsUI(VisualElement rootElement)
        {
            VisualElement contentContainer = rootElement.Q<VisualElement>("content-container");
            if (contentContainer == null)
            {
                ConvaiLogger.Warn("Cannot find Content Container", ConvaiLogger.LogCategory.UI);
                return;
            }

            VisualElement loggerTable = contentContainer.Q<VisualElement>("logger-table");
            if (loggerTable == null)
            {
                ConvaiLogger.Warn("Cannot find loggerTable", ConvaiLogger.LogCategory.UI);
                return;
            }

            _loggerSettingsLogic = new LoggerSettingsLogic();
            CreateLoggerTable(contentContainer);
            SetupButtons(contentContainer);
        }

        private void CreateLoggerTable(VisualElement contentContainer)
        {
            VisualElement loggerSettings = contentContainer.Q<VisualElement>("logger-settings");
            VisualElement loggerTable = loggerSettings.Q<VisualElement>("logger-table");
            CreateHeaders(loggerTable);
            VisualElement lastRow = null;
            foreach (FieldInfo fieldInfo in typeof(Runtime.LoggerSystem.LoggerSettings).GetFields())
            {
                VisualElement tableRow = new();
                Label categoryName = new(fieldInfo.Name);
                Toggle selectAll = CreateSelectAllForCategory(fieldInfo);
                _loggerSettingsLogic.AddToSelectAllDictionary(fieldInfo, selectAll);
                categoryName.AddToClassList("logger-table-element");
                tableRow.Add(selectAll);
                tableRow.Add(categoryName);
                CreateSeverityTogglesForCategory(fieldInfo, tableRow);
                tableRow.AddToClassList("logger-table-row");
                loggerTable.Add(tableRow);
                lastRow = tableRow;
            }

            lastRow?.AddToClassList("logger-table-row-last");
        }

        private static void CreateHeaders(VisualElement loggerTable)
        {
            VisualElement tableHeader = new();
            VisualElement selectAll = new Label("Select All");
            VisualElement category = new Label("Category");
            selectAll.AddToClassList("logger-table-element");
            category.AddToClassList("logger-table-element");
            tableHeader.Add(selectAll);
            tableHeader.Add(category);
            foreach (ConvaiLogger.LogLevel logLevel in Enum.GetValues(typeof(ConvaiLogger.LogLevel)))
            {
                if (logLevel == ConvaiLogger.LogLevel.None) continue;
                VisualElement label = new Label(logLevel.ToString());
                label.AddToClassList("logger-table-element");
                tableHeader.Add(label);
            }

            tableHeader.AddToClassList("logger-table-row");
            tableHeader.AddToClassList("logger-table-row-first");
            loggerTable.Add(tableHeader);
        }

        private Toggle CreateSelectAllForCategory(FieldInfo fieldInfo)
        {
            Toggle selectAll = new()
            {
                value = _loggerSettingsLogic.IsAllSelectedForCategory(fieldInfo)
            };
            selectAll.RegisterValueChangedCallback(evt => { _loggerSettingsLogic.OnSelectAllClicked(fieldInfo, evt.newValue); });
            selectAll.AddToClassList("logger-table-element");
            return selectAll;
        }

        private void CreateSeverityTogglesForCategory(FieldInfo fieldInfo, VisualElement severityContainer)
        {
            foreach (ConvaiLogger.LogLevel enumType in Enum.GetValues(typeof(ConvaiLogger.LogLevel)))
            {
                if (enumType == ConvaiLogger.LogLevel.None) continue;
                Toggle toggle = new()
                {
                    value = _loggerSettingsLogic.GetLogLevelEnabledStatus(fieldInfo, enumType)
                };

                void Callback(ChangeEvent<bool> evt)
                {
                    _loggerSettingsLogic.OnToggleClicked(fieldInfo, enumType, evt.newValue);
                }

                toggle.UnregisterValueChangedCallback(Callback);
                toggle.RegisterValueChangedCallback(Callback);
                toggle.AddToClassList("logger-table-element");
                severityContainer.Add(toggle);
                _loggerSettingsLogic.AddToToggleDictionary(fieldInfo, toggle);
            }

            severityContainer.AddToClassList("severity-container");
        }

        private void SetupButtons(VisualElement content)
        {
            VisualElement loggerSettings = content.Q<VisualElement>("logger-settings");
            if (loggerSettings == null)
            {
                ConvaiLogger.Warn("Cannot find logger-settings", ConvaiLogger.LogCategory.UI);
                return;
            }

            Button selectAll = loggerSettings.Q<Button>("select-all");
            if (selectAll != null)
            {
                selectAll.clicked -= _loggerSettingsLogic.SelectAllOnClicked;
                selectAll.clicked += _loggerSettingsLogic.SelectAllOnClicked;
            }

            Button clearAll = loggerSettings.Q<Button>("clear-all");
            if (clearAll != null)
            {
                clearAll.clicked -= _loggerSettingsLogic.ClearAllOnClicked;
                clearAll.clicked += _loggerSettingsLogic.ClearAllOnClicked;
            }
        }
    }
}