using System;
using System.Collections.Generic;
using Assets.Convai.Scripts.Editor.Setup.LongTermMemory;
using Convai.Scripts.Editor.CustomPackage;
using Convai.Scripts.Editor.Setup.AccountsSection;
using Convai.Scripts.Editor.Setup.CharacterImporter;
using Convai.Scripts.Editor.Setup.Documentation;
using Convai.Scripts.Editor.Setup.LoggerSettings;
using Convai.Scripts.Editor.Setup.Updates;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup {
    public class ConvaiSDKSetupEditorWindow : EditorWindow {
        private const string STYLE_SHEET_PATH = "Assets/Convai/Art/UI/Editor/ConvaiSDKSetupWindow.uss";
        private const string VISUAL_TREE_PATH = "Assets/Convai/Art/UI/Editor/ConvaiSDKSetupWindow.uxml";
        private static VisualElement _contentContainer;
        private static VisualElement _root;
        private static readonly Dictionary<string, (VisualElement Section, Button Button)> Sections = new();

        private static readonly string[] SectionNames =
            { "welcome", "account", "package-management", "character-importer", "logger-settings", "updates", "documentation", "contact-us", "ltm" };

        private static readonly HashSet<string> ApiKeyDependentSections = new() { "character-importer", "logger-settings", "package-management", "ltm" };

        private static bool _isApiKeySet;

        public static event Action OnAPIKeySet;
        public static bool IsApiKeySet {
            get => _isApiKeySet;
            set {
                _isApiKeySet = value;
                OnAPIKeySet?.Invoke();
            }
        }

        private void CreateGUI () {
            InitializeUI();
            SetupNavigationHandlers();
        }

        [MenuItem( "Convai/Welcome", priority = 1 )]
        public static void OpenWindow () {
            OpenSection( "welcome" );
        }

        [MenuItem( "Convai/API Key Setup", priority = 2 )]
        public static void OpenAPIKeySetup () {
            OpenSection( "account" );
        }
#if READY_PLAYER_ME
        [MenuItem( "Convai/Character Importer", priority = 3 )]
        public static void OpenCharacterImporter () {
            OpenSection( "character-importer" );
        }
#endif

        [MenuItem( "Convai/Logger Settings", priority = 4 )]
        public static void OpenLoggerSettings () {
            OpenSection( "logger-settings" );
        }

        [MenuItem( "Convai/Custom Package Installer", priority = 5 )]
        public static void OpenCustomPackageInstaller () {
            OpenSection( "package-management" );
        }

        [MenuItem( "Convai/Documentation", priority = 6 )]
        public static void OpenDocumentation () {
            OpenSection( "documentation" );
        }

        [MenuItem( "Convai/Long Term Memory", priority = 6 )]
        public static void OpenLTM () {
            OpenSection( "ltm" );
        }

        private static void OpenSection ( string sectionName ) {
            Rect rect = new( 100, 100, 1200, 550 );
            ConvaiSDKSetupEditorWindow window = GetWindowWithRect<ConvaiSDKSetupEditorWindow>( rect, true, "Convai SDK Setup", true );
            window.minSize = window.maxSize = rect.size;
            window.Show();
            ShowSection( sectionName );
        }

        private void InitializeUI () {
            _root = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>( VISUAL_TREE_PATH );
            _root.Add( visualTree.Instantiate() );

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>( STYLE_SHEET_PATH );
            _root.styleSheets.Add( styleSheet );

            _contentContainer = _root.Q<VisualElement>( "content-container" );

            InitializeSections();

            _ = new APIKeySetupUI( _root );
            _ = new AccountInformationUI( _root );
#if READY_PLAYER_ME
            _ = new CharacterImporterUI( _root, FindObjectOfType<ConvaiChatUIHandler>() );
#endif
            _ = new LoggerSettingsUI( _root );
            _ = new DocumentationUI( _root );
            _ = new UpdatesSectionUI( _root );
            _ = new ConvaiCustomPackageInstaller( _root );
            _ = new LongTermMemoryUI( _root );

            _root.Q<Button>( "documentation-page" ).clicked += () => Application.OpenURL( "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin" );
        }

        private static void InitializeSections () {
            foreach ( string section in SectionNames )
                Sections[section] = (_contentContainer.Q( section ), _root.Q<Button>( $"{section}-btn" ));
        }

        private static void SetupNavigationHandlers () {
            foreach ( KeyValuePair<string, (VisualElement Section, Button Button)> section in Sections )
                section.Value.Button.clicked += () => ShowSection( section.Key );
        }

        public static void ShowSection ( string sectionName ) {
            if ( !IsApiKeySet && ApiKeyDependentSections.Contains( sectionName ) ) {
                EditorUtility.DisplayDialog( "API Key Required", "Please set up your API Key to access this section.", "OK" );
                return;
            }

#if !READY_PLAYER_ME
            if (sectionName == "character-importer")
            {
                EditorUtility.DisplayDialog("Character Importer", "Character Importer feature works only when Ready Player Me SDK is installed in the project. Please install it and try again", "OK");
                return;
            }
#endif

            if ( Sections.TryGetValue( sectionName, out (VisualElement Section, Button Button) sectionData ) ) {
                foreach ( (VisualElement Section, Button Button) section in Sections.Values )
                    section.Section.style.display = section.Section == sectionData.Section ? DisplayStyle.Flex : DisplayStyle.None;

                UpdateNavigationState( sectionName );
            }
            else {
                ConvaiLogger.Warn( $"Section '{sectionName}' not found.", ConvaiLogger.LogCategory.Character );
            }
        }

        private static void UpdateNavigationState ( string activeSectionName ) {
            foreach ( KeyValuePair<string, (VisualElement Section, Button Button)> section in Sections ) {
                bool isActive = section.Key == activeSectionName;
                section.Value.Button.EnableInClassList( "sidebar-link--active", isActive );
                section.Value.Button.EnableInClassList( "sidebar-link", !isActive );
            }
        }
    }
}