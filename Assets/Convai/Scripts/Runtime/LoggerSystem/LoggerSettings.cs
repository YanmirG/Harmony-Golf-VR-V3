using UnityEngine;

namespace Convai.Scripts.Runtime.LoggerSystem
{
    [CreateAssetMenu(fileName = "LoggerSettings", menuName = "Convai/LoggerSettings")]
    public class LoggerSettings : ScriptableObject
    {
        public ConvaiLogger.LogLevel LipSync;
        public ConvaiLogger.LogLevel Character;
        public ConvaiLogger.LogLevel Actions;
        public ConvaiLogger.LogLevel UI;
        public ConvaiLogger.LogLevel GRPC;
        public ConvaiLogger.LogLevel Editor;
    }
}