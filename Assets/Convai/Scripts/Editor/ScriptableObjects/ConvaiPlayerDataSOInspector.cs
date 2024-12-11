using System.Text;
using Convai.Scripts.Runtime.PlayerStats;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.ScriptableObjects
{
    [CustomEditor(typeof(ConvaiPlayerDataSO))]
    public class ConvaiPlayerDataSOInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ConvaiPlayerDataSO dataSO = (ConvaiPlayerDataSO)target;
            ResetData(dataSO);
            CopyData(dataSO);
            GUILayout.Space(10);
            GUILayout.Label("Player Pref Settings");
            GUILayout.BeginHorizontal();
            LoadFromPlayerPref(dataSO);
            SaveDataToPlayerPrefs(dataSO);
            DeleteFromPlayerPrefs(dataSO);
            GUILayout.EndHorizontal();
        }

        private static void SaveDataToPlayerPrefs(ConvaiPlayerDataSO dataSO)
        {
            if (GUILayout.Button("Save"))
            {
                PlayerPrefs.SetString(ConvaiPlayerDataHandler.PLAYER_NAME_SAVE_KEY, dataSO.PlayerName);
                PlayerPrefs.SetString(ConvaiPlayerDataHandler.SPEAKER_ID_SAVE_KEY, dataSO.SpeakerID);
            }
        }

        private static void LoadFromPlayerPref(ConvaiPlayerDataSO dataSO)
        {
            if (GUILayout.Button("Load"))
            {
                dataSO.PlayerName = PlayerPrefs.GetString(ConvaiPlayerDataHandler.PLAYER_NAME_SAVE_KEY, dataSO.DefaultPlayerName);
                dataSO.SpeakerID = PlayerPrefs.GetString(ConvaiPlayerDataHandler.SPEAKER_ID_SAVE_KEY, string.Empty);
            }
        }

        private static void DeleteFromPlayerPrefs(ConvaiPlayerDataSO dataSO)
        {
            if (GUILayout.Button("Delete"))
            {
                dataSO.PlayerName = string.Empty;
                dataSO.SpeakerID = string.Empty;

                PlayerPrefs.DeleteKey(ConvaiPlayerDataHandler.PLAYER_NAME_SAVE_KEY);
                PlayerPrefs.DeleteKey(ConvaiPlayerDataHandler.SPEAKER_ID_SAVE_KEY);
            }
        }

        private static void CopyData(ConvaiPlayerDataSO dataSO)
        {
            if (GUILayout.Button("Copy Data"))
            {
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine($"PlayerName: {dataSO.PlayerName}");
                stringBuilder.AppendLine($"Speaker ID: {dataSO.SpeakerID}");
                GUIUtility.systemCopyBuffer = stringBuilder.ToString();
            }
        }

        private static void ResetData(ConvaiPlayerDataSO dataSO)
        {
            if (GUILayout.Button("Reset Data"))
            {
                dataSO.PlayerName = string.Empty;
                dataSO.SpeakerID = string.Empty;
            }
        }
    }
}