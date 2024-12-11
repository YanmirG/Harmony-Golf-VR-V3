#if !READY_PLAYER_ME
using UnityEditor;
using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Convai.Scripts.Editor.CustomPackage
{
    [InitializeOnLoad]
    public class ReadyPlayerMeImporter
    {
        private static AddRequest _request;

        static ReadyPlayerMeImporter()
        {
            Debug.Log("Ready Player Me is not installed, importing it.");
            _request = Client.Add("https://github.com/readyplayerme/rpm-unity-sdk-core.git");
            EditorUtility.DisplayProgressBar("Importing Ready Player Me", "Importing.....", Random.Range(0, 1f));
            EditorApplication.update += UnityEditorUpdateCallback;
        }

        private static void UnityEditorUpdateCallback()
        {
            if (_request == null) return;
            if (!_request.IsCompleted) return;
            switch (_request.Status)
            {
                case StatusCode.Success:
                    Debug.Log("Ready Player Me has been imported successfully");
                    break;
                case StatusCode.Failure:
                    Debug.LogError("Ready Player Me has failed to import: " + _request.Error.message);
                    break;
                case StatusCode.InProgress:
                    Debug.Log("Ready Player Me is still importing...");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorApplication.update -= UnityEditorUpdateCallback;
            EditorUtility.ClearProgressBar();
        }
    }
}
#endif