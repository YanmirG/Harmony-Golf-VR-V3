using System.Collections.Generic;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.Documentation
{
    public class DocumentationUI
    {
        private readonly VisualElement _root;

        public DocumentationUI(VisualElement root)
        {
            _root = root;
            SetupLinkHandlers();
        }

        private void SetupLinkHandlers()
        {
            Dictionary<string, string> links = new()
            {
                { "unity-plugin-setup", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/setting-up-unity-plugin" },
                { "quick-start-tutorial", "https://youtu.be/anb9ityi0MQ" },
                { "video-tutorials", "https://www.youtube.com/playlist?list=PLn_7tCx0ChipYHtbe8yzdV5kMbozN2EeB" },

                { "narrative-design", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/adding-narrative-design-to-your-character" },
                { "transcript-system", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/utilities/transcript-ui-system" },
                { "actions", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/adding-actions-to-your-character" },
                { "npc-interaction", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/adding-npc-to-npc-conversation" },
                { "facial-expressions", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/adding-lip-sync-to-your-character" },


                { "platform-specific-builds", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/building-for-supported-platforms" },
                { "ios-build", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/building-for-supported-platforms/building-for-ios-ipados" },
                {
                    "macos-build",
                    "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/building-for-supported-platforms/microphone-permission-issue-on-intel-macs-with-universal-builds"
                },
                { "ar-vr-build", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/building-for-supported-platforms/building-for-ar" },

                { "faq", "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/troubleshooting-guide" },

                { "convai-support", "https://convai.com/contact" },
                { "discord", "https://discord.gg/convai" }
            };

            foreach (KeyValuePair<string, string> link in links) SetupLinkHandler(link.Key, link.Value);
        }


        private void SetupLinkHandler(string elementName, string url)
        {
            VisualElement element = _root.Q<VisualElement>(elementName);
            switch (element)
            {
                case null:
                    ConvaiLogger.Warn($"Element '{elementName}' not found.", ConvaiLogger.LogCategory.UI);
                    return;
                case Button button:
                    button.clicked += () => Application.OpenURL(url);
                    break;
                default:
                    element.AddManipulator(new Clickable(() => Application.OpenURL(url)));
                    break;
            }
        }
    }
}