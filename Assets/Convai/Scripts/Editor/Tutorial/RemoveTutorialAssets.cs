using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Convai.Scripts.Editor.Tutorial
{
    public class RemoveTutorialAssets : EditorWindow
    {
        private const string TUTORIAL_ASSETS_PATH = "Assets/Convai/Tutorials";
        private const string TUTORIAL_PACKAGE_NAME = "com.unity.learn.iet-framework";

        [MenuItem("Convai/Remove Tutorial Assets")]
        private static void StartUninstallProcess()
        {
            if (Directory.Exists(TUTORIAL_ASSETS_PATH))
            {
                AssetDatabase.DeleteAsset(TUTORIAL_ASSETS_PATH);
                AssetDatabase.Refresh();

                foreach (PackageInfo packageInfo in PackageInfo.GetAllRegisteredPackages())
                    // Check if the package name matches
                    if (packageInfo.name == TUTORIAL_PACKAGE_NAME)
                        Client.Remove(TUTORIAL_PACKAGE_NAME);
            }
        }
    }
}