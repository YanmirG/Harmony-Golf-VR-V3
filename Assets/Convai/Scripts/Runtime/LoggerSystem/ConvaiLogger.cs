using System;
using System.Collections.Generic;
using System.Diagnostics;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Convai.Scripts.Runtime.LoggerSystem
{
    public static class ConvaiLogger
    {
        #region LogCategory enum

        public enum LogCategory
        {
            Character,
            LipSync,
            Actions,
            Editor,
            UI,
            GRPC
        }

        #endregion

        private static readonly Dictionary<LogLevel, string> LevelColors = new()
        {
            { LogLevel.Debug, "cyan" },
            { LogLevel.Info, "grey" },
            { LogLevel.Warning, "yellow" },
            { LogLevel.Error, "red" },
            { LogLevel.Exception, "orange" }
            //List of available colours at: https://docs.unity3d.com/Manual/StyledText.html
        };

        private static bool ShouldLog(LogLevel level, LogCategory category)
        {
            return category switch
            {
                LogCategory.Character => (LoggerConfig.CharacterResponseFlag & level) != 0,
                LogCategory.LipSync => (LoggerConfig.LipsyncLogFlag & level) != 0,
                LogCategory.Actions => (LoggerConfig.ActionFlag & level) != 0,
                LogCategory.UI => (LoggerConfig.UIFlag & level) != 0,
                LogCategory.GRPC => (LoggerConfig.GRPCFlag & level) != 0,
                LogCategory.Editor => (LoggerConfig.EditorFlag & level) != 0,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }

        private static bool IsStringJson(string message)
        {
            string trimmedMessage = message.Trim();
            return trimmedMessage.StartsWith("{") || trimmedMessage.StartsWith("[");
        } // ReSharper disable Unity.PerformanceAnalysis
        private static void LogMessage(string message, LogLevel level, LogCategory category, params object[] args)
        {
            if (!ShouldLog(level, category)) return;

            string formattedMessage = IsStringJson(message) ? $"{message}" : string.Format(message, args);

            string logMessage =
                $"[{Enum.GetName(typeof(LogLevel), level)}][{Enum.GetName(typeof(LogCategory), category)}]: {formattedMessage}";

            if (LevelColors.TryGetValue(level, out string color) && color != "default")
                logMessage = $"<color={color}>{logMessage}</color>";

            // Select the first frame that is from the non-ConvaiLogger type.
            StackTrace trace = new(2, true);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                if (frame.GetMethod().ReflectedType != typeof(ConvaiLogger))
                {
                    logMessage +=
                        $"\n[Stack Trace - Method: {frame.GetMethod().Name}, at Line: {frame.GetFileLineNumber()} in File: {frame.GetFileName()}]";
                    break;
                }
            }

            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Info:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(logMessage);
                    break;
                case LogLevel.Exception:
                    Debug.LogError(logMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        public static void Info(object message, LogCategory category)
        {
            Info(message.ToString(), category);
        }

        public static void DebugLog(object message, LogCategory category)
        {
            DebugLog(message.ToString(), category);
        }

        public static void Warn(object message, LogCategory category)
        {
            Warn(message.ToString(), category);
        }

        public static void Error(object message, LogCategory category)
        {
            Error(message.ToString(), category);
        }

        public static void Info(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Info, category, args);
        }

        public static void DebugLog(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Debug, category, args);
        }

        public static void Warn(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Warning, category, args);
        }

        public static void Error(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Error, category, args);
        }

        public static void Exception(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Exception, category, args);
        }

        public static void Exception(Exception ex, LogCategory category)
        {
            string message = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Exception(message, category);
        }

        [Flags]
        public enum LogLevel
        {
            None = 0,
            Debug = 1 << 0,
            Error = 1 << 1,
            Exception = 1 << 2,
            Info = 1 << 3,
            Warning = 1 << 4
        }
    }
}

namespace Convai.Scripts.Runtime.Addons
{
    public static class LoggerConfig
    {
        private static LoggerSettings _settings;

        public static readonly ConvaiLogger.LogLevel LipsyncLogFlag = Settings.LipSync;
        public static readonly ConvaiLogger.LogLevel CharacterResponseFlag = _settings.Character;
        public static readonly ConvaiLogger.LogLevel ActionFlag = _settings.Actions;
        public static readonly ConvaiLogger.LogLevel UIFlag = _settings.UI;
        public static readonly ConvaiLogger.LogLevel GRPCFlag = _settings.GRPC;
        public static readonly ConvaiLogger.LogLevel EditorFlag = _settings.Editor;


        private static LoggerSettings Settings
        {
            get
            {
                if (_settings == null) _settings = Resources.Load<LoggerSettings>("LoggerSettings");

                return _settings;
            }
        }
    }
}