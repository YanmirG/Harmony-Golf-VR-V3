using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.Updates
{
    public class UpdatesSectionUI
    {
        private readonly VisualElement _root;

        public UpdatesSectionUI(VisualElement root)
        {
            _root = root;
            SetupUpdateSectionHandlers();
            SetInitialVersionNumber();
        }

        private void SetupUpdateSectionHandlers()
        {
            // Button checkUpdatesButton = _root.Q<Button>("check-updates");
            // if (checkUpdatesButton != null)
            //     checkUpdatesButton.clicked += CheckForUpdates;

            Button viewFullChangelogButton = _root.Q<Button>("view-full-changelog");
            if (viewFullChangelogButton != null)
                viewFullChangelogButton.clicked += () => Application.OpenURL("https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/changelogs");
        }

        private void SetInitialVersionNumber()
        {
            Label versionLabel = _root.Q<Label>("current-version");
            if (versionLabel != null)
                versionLabel.text = "v3.1.0";
        }

        private void CheckForUpdates()
        {
            ConvaiLogger.DebugLog("Checking for updates...", ConvaiLogger.LogCategory.UI);
            UpdateStatusMessage("Checking for updates...");
        }

        private void UpdateStatusMessage(string message)
        {
            Label statusLabel = _root.Q<Label>("update-message");
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}