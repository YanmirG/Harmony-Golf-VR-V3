using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.AccountsSection
{
    public class APIKeyReferralWindow : EditorWindow
    {
        private static APIKeyReferralWindow _window;

        private void OnEnable()
        {
            CreateAPIKeyReferralWindow();
        }

        public static void ShowWindow()
        {
            _window = GetWindow<APIKeyReferralWindow>();
            _window.titleContent = new GUIContent("Step 2: Referral Source");
            _window.minSize = new Vector2(300, 200);
            _window.Show();
        }

        private void CreateAPIKeyReferralWindow()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            Label attributionSourceLabel = new("[Step 2/2] Where did you discover Convai?")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            List<string> attributionSourceOptions = new()
            {
                "Search Engine (Google, Bing, etc.)",
                "Youtube",
                "Social Media (Facebook, Instagram, TikTok, etc.)",
                "Friend Referral",
                "Unity Asset Store",
                "Others"
            };

            TextField otherOptionTextField = new();

            ToolbarMenu toolbarMenu = new() { text = "Click here to select option..." };

            foreach (string choice in attributionSourceOptions)
                toolbarMenu.menu.AppendAction(choice,
                    action =>
                    {
                        _ = choice;
                        toolbarMenu.text = choice;
                    });

            toolbarMenu.style.paddingBottom = 10;
            toolbarMenu.style.paddingLeft = 30;
            toolbarMenu.style.paddingRight = 30;
            toolbarMenu.style.paddingTop = 10;

            Button continueButton = new(ClickEvent)
            {
                text = "Continue",
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    alignSelf = Align.Center,
                    paddingBottom = 5,
                    paddingLeft = 30,
                    paddingRight = 30,
                    paddingTop = 5
                }
            };

            root.Add(new Label("\n"));
            root.Add(attributionSourceLabel);
            root.Add(new Label("\n"));
            root.Add(toolbarMenu);
            root.Add(new Label("\nIf selected Others above, please specify from where: "));
            root.Add(otherOptionTextField);
            root.Add(new Label("\n"));
            root.Add(continueButton);

            root.style.marginBottom = 20;
            root.style.marginLeft = 20;
            root.style.marginRight = 20;
            root.style.marginTop = 20;
            return;

            async void ClickEvent()
            {
                await APIKeySetupLogic.ContinueEvent(toolbarMenu.text, otherOptionTextField.text, _window);
            }
        }
    }
}