using System;
using System.Reflection;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.Logger
{
    /// <summary>
    ///     Manages the settings for the ConvaiLogger, including loading, creating, and modifying LoggerSettings.
    /// </summary>
    public class LoggerSettingsManager
    {
        // Path to the LoggerSettings asset
        private const string SETTINGS_PATH = "Assets/Convai/Resources/LoggerSettings.asset";

        /// <summary>
        ///     The LoggerSettings instance.
        /// </summary>
        private LoggerSettings _loggerSettings;

        /// <summary>
        ///     Property accessor for _loggerSettings. If _loggerSettings is null, it attempts to load it from the asset path.
        ///     If the asset does not exist, it creates a new LoggerSettings instance.
        /// </summary>
        public LoggerSettings LoggerSettings
        {
            get
            {
                if (_loggerSettings == null)
                {
                    _loggerSettings = AssetDatabase.LoadAssetAtPath<LoggerSettings>(SETTINGS_PATH);
                    if (_loggerSettings == null)
                    {
                        CreateLoggerSettings();
                        ConvaiLogger.Warn("LoggerSettings ScriptableObject not found. Creating one...",
                            ConvaiLogger.LogCategory.Character);
                    }
                }

                return _loggerSettings;
            }
        }

        /// <summary>
        ///     Creates a new LoggerSettings instance with default values and saves it as an asset
        /// </summary>
        private void CreateLoggerSettings()
        {
            _loggerSettings = ScriptableObject.CreateInstance<LoggerSettings>();

            _loggerSettings.Character = ConvaiLogger.LogLevel.None
                                        | ConvaiLogger.LogLevel.Debug
                                        | ConvaiLogger.LogLevel.Error
                                        | ConvaiLogger.LogLevel.Exception
                                        | ConvaiLogger.LogLevel.Info
                                        | ConvaiLogger.LogLevel.Warning;

            _loggerSettings.LipSync = ConvaiLogger.LogLevel.None
                                      | ConvaiLogger.LogLevel.Debug
                                      | ConvaiLogger.LogLevel.Error
                                      | ConvaiLogger.LogLevel.Exception
                                      | ConvaiLogger.LogLevel.Info
                                      | ConvaiLogger.LogLevel.Warning;

            _loggerSettings.Actions = ConvaiLogger.LogLevel.None
                                      | ConvaiLogger.LogLevel.Debug
                                      | ConvaiLogger.LogLevel.Error
                                      | ConvaiLogger.LogLevel.Exception
                                      | ConvaiLogger.LogLevel.Info
                                      | ConvaiLogger.LogLevel.Warning;

            // Check if the Convai folder exists and create if not
            if (!AssetDatabase.IsValidFolder("Assets/Convai/Resources"))
                AssetDatabase.CreateFolder("Assets/Convai", "Resources");

            // Check if the Settings folder exists and create if not
            if (!AssetDatabase.IsValidFolder("Assets/Convai/Resources/Settings"))
                AssetDatabase.CreateFolder("Assets/Convai/Resources", "Settings");

            AssetDatabase.CreateAsset(_loggerSettings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        ///     Checks if all flags for a given row are set.
        /// </summary>
        /// <returns>True if all flags for the given row are set, false otherwise.</returns>
        public bool GetAllFlagsForRow(FieldInfo fieldInfo)
        {
            if (fieldInfo == null) return false;
            ConvaiLogger.LogLevel allLevel = AllLevel();
            ConvaiLogger.LogLevel currentValue = (ConvaiLogger.LogLevel)fieldInfo.GetValue(_loggerSettings);
            return allLevel == currentValue;
        }

        private static ConvaiLogger.LogLevel AllLevel()
        {
            ConvaiLogger.LogLevel allLevel = ConvaiLogger.LogLevel.None;
            foreach (ConvaiLogger.LogLevel logLevel in Enum.GetValues(typeof(ConvaiLogger.LogLevel))) allLevel |= logLevel;

            return allLevel;
        }


        /// <summary>
        ///     Renders a checkbox for a given log type and handles changes to its value.
        /// </summary>
        public void RenderAndHandleCheckbox(FieldInfo field)
        {
            if (field == null) return;
            foreach (ConvaiLogger.LogLevel enumValue in Enum.GetValues(typeof(ConvaiLogger.LogLevel)))
            {
                if (enumValue == ConvaiLogger.LogLevel.None) continue;
                ConvaiLogger.LogLevel rowFlag = (ConvaiLogger.LogLevel)field.GetValue(_loggerSettings);
                bool currentLevelValue = (rowFlag & enumValue) != 0;
                bool newValue = EditorGUILayout.Toggle(currentLevelValue, GUILayout.Width(100));
                if (newValue)
                    rowFlag |= enumValue;
                else
                    rowFlag &= ~enumValue;
                field.SetValue(_loggerSettings, rowFlag);
            }
        }


        /// <summary>
        ///     Sets all flags for a given row to the provided value.
        /// </summary>
        /// <param name="rowName">The name of the row to set the flags for.</param>
        /// <param name="value">The value to set all flags to.</param>
        public void SetAllFlagsForRow(FieldInfo rowName, bool value)
        {
            rowName.SetValue(_loggerSettings, value ? 31 : 0);
        }


        /// <summary>
        ///     Sets all flags to the provided value.
        /// </summary>
        /// <param name="value"> The value to set all flags to.</param>
        public void SetAllFlags(bool value)
        {
            foreach (FieldInfo fieldInfo in _loggerSettings.GetType().GetFields()) fieldInfo.SetValue(_loggerSettings, value ? 31 : 0);
        }
    }
}