using Assets.Convai.Scripts.Runtime.PlayerStats.API.Model;
using Convai.Scripts.Editor.Setup;
using Convai.Scripts.Runtime.PlayerStats.API;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Convai.Scripts.Editor.Setup.LongTermMemory {
    internal class LongTermMemoryUI {
        private VisualElement _uiContainer;
        private VisualElement _listContainer;
        private VisualElement _disclaimerContainer;
        private Button _refreshButton;
        private Button _toggleDisclaimerButton;
        private Label _noSpeakerId;
        internal LongTermMemoryUI ( VisualElement root ) {
            Initialize( root );
            ToggleDisclaimerButton_Clicked(forceShow: true);
            if(ConvaiSDKSetupEditorWindow.IsApiKeySet) RefreshSpeakerList();
            ConvaiSDKSetupEditorWindow.OnAPIKeySet += RefreshSpeakerList;
            _refreshButton.clicked += RefreshSpeakerList;
            _toggleDisclaimerButton.clicked += () => { ToggleDisclaimerButton_Clicked( false ); };
        }

        private void ToggleDisclaimerButton_Clicked (bool forceShow) {
            if ( forceShow ) {
                ToggleVisibility( _disclaimerContainer, true );
                _toggleDisclaimerButton.text = "Hide";
                return;
            }
            ToggleVisibility( _disclaimerContainer, !_disclaimerContainer.visible );
            _toggleDisclaimerButton.text = _disclaimerContainer.visible ? "Hide" : "Show";
        }

        ~LongTermMemoryUI () {
            ConvaiSDKSetupEditorWindow.OnAPIKeySet -= RefreshSpeakerList;
            _refreshButton.clicked -= RefreshSpeakerList;
        }


        private void Initialize ( VisualElement root ) {
            _uiContainer = root.Q<VisualElement>( "content-container" ).Q<VisualElement>( "ltm" );
            Debug.Assert( _uiContainer != null, "UI Container cannot be found, something went wrong" );
            _listContainer = _uiContainer.Q<VisualElement>( "container" );
            Debug.Assert( _listContainer != null, "List Container cannot be found, something went wrong" );
            _disclaimerContainer = _uiContainer.Q<VisualElement>( "disclaimer-content" );
            Debug.Assert( _disclaimerContainer != null, "Disclaimer Container not found" );
            _refreshButton = _uiContainer.Q<Button>( "refresh-btn" );
            Debug.Assert( _refreshButton != null, "Cannot find Refresh Button" );
            _toggleDisclaimerButton = _uiContainer.Q<Button>( "disclaimer-toggle-button" );
            Debug.Assert( _toggleDisclaimerButton != null, "Toggle Disclaimer Button Not found" );
            _noSpeakerId = _uiContainer.Q<Label>( "no-speaker-id-label" );
            Debug.Assert( _noSpeakerId != null, "Cannot find No Speaker ID Label" );
        }

        public async void RefreshSpeakerList () {
            if ( !ConvaiAPIKeySetup.GetAPIKey( out string apiKey ) )
                return;
            ToggleVisibility( _noSpeakerId, false );
            List<SpeakerIDDetails> speakerIDs = await LongTermMemoryAPI.GetSpeakerIDList( apiKey );
            RefreshSpeakerList( speakerIDs );
        }


        private void RefreshSpeakerList ( List<SpeakerIDDetails> speakerIDs ) {
            _listContainer.Clear();
            if(speakerIDs.Count == 0 ) {
                ToggleVisibility( _noSpeakerId, true );
                ToggleVisibility(_listContainer, false );
                return;
            }
            foreach ( SpeakerIDDetails sid in speakerIDs ) {
                VisualElement item = new VisualElement() {
                    name = "item",
                };
                item.AddToClassList( "ltm-item" );
                item.Add( GetInformation( sid ) );
                item.Add( GetButtonContainer( sid ) );
                _listContainer.Add( item );
            }
        }

        private static VisualElement GetInformation ( SpeakerIDDetails sid ) {
            VisualElement visualElement = new VisualElement() {
                name = "information",
            };
            visualElement.AddToClassList( "ltm-information" );
            visualElement.Add( AddLabel( $"Name: {sid.Name}" ) );
            visualElement.Add( AddLabel( $"Speaker ID: {sid.ID}" ) );
            return visualElement;
        }

        private static Label AddLabel ( string value ) {
            Label label = new Label( value );
            label.AddToClassList( "ltm-label" );
            return label;
        }

        private VisualElement GetButtonContainer ( SpeakerIDDetails sid ) {
            VisualElement visualElement = new VisualElement() {
                name = "button-container"
            };
            visualElement.AddToClassList( "ltm-button-container" );
            Button button = new Button() {
                name = "delete-btn",
                text = "Delete"
            };
            button.AddToClassList( "button-small" );
            button.AddToClassList( "ltm-button" );
            button.clicked += () => { SpeakerID_Delete_Clicked( sid ); };
            visualElement.Add( button );
            return visualElement;
        }

        private async void SpeakerID_Delete_Clicked ( SpeakerIDDetails details ) {
            if ( !ConvaiAPIKeySetup.GetAPIKey( out string apiKey ) )
                return;
            ToggleVisibility(_listContainer, false );
            bool result = await LongTermMemoryAPI.DeleteSpeakerID( apiKey, details.ID );
            ToggleVisibility( _listContainer, true );
            _listContainer.Clear();
            if ( !result )
                return;
            RefreshSpeakerList();
        }

        private static void ToggleVisibility ( VisualElement element, bool visible ) {
            element.visible = visible;
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

    }
}
