using Convai.Scripts.Editor.Setup;
using UnityEditor;

namespace Convai.Scripts.Editor.Utils
{
    [InitializeOnLoad]
    public class ConvaiAPIKeySetupEditor
    {
        static ConvaiAPIKeySetupEditor()
        {
            ConvaiAPIKeySetup.OnAPIKeyNotFound += ShowAPIKeyNotFoundDialog;
        }

        private static void ShowAPIKeyNotFoundDialog()
        {
            ConvaiSDKSetupEditorWindow window = EditorWindow.GetWindow<ConvaiSDKSetupEditorWindow>();
            ConvaiSDKSetupEditorWindow.ShowSection("account");
        }
    }
}