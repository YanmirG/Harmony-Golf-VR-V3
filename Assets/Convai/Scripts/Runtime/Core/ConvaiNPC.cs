using System;
using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.Attributes;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.PlayerStats;
using Convai.Scripts.Runtime.UI;
using Grpc.Core;
using Service;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using ConvaiLipSync = Convai.Scripts.Runtime.Features.LipSync.ConvaiLipSync;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    ///     The ConvaiNPC class is a MonoBehaviour script that gives a GameObject the ability to interact with the Convai API.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(AudioSource))]
    [AddComponentMenu("Convai/ConvaiNPC")]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/overview-of-the-convainpc.cs-script")]
    public class ConvaiNPC : MonoBehaviour
    {
        private const int AUDIO_SAMPLE_RATE = 44100;
        private const string GRPC_API_ENDPOINT = "stream.convai.com";
        private const int RECORDING_FREQUENCY = AUDIO_SAMPLE_RATE;
        private const int RECORDING_LENGTH = 30;
        private static readonly int Talk = Animator.StringToHash("Talk");

        [Header("Character Information")] [Tooltip("Enter the character name for this NPC.")]
        public string characterName;

        [Tooltip("Enter the character ID for this NPC.")]
        public string characterID;

        [Tooltip("The current session ID for the chat with this NPC.")] [ReadOnly]
        public string sessionID = "-1";

        [Tooltip("Is this character active?")] [ReadOnly]
        public bool isCharacterActive;

        [HideInInspector] public ConvaiActionsHandler actionsHandler;
        [HideInInspector] public ConvaiLipSync convaiLipSync;

        [Tooltip("Is this character talking?")] [SerializeField] [ReadOnly]
        private bool isCharacterTalking;

        [Header("Session Initialization")] [Tooltip("Enable/disable initializing session ID by sending a text request to the server")]
        public bool initializeSessionID;

        [HideInInspector] public ConvaiPlayerInteractionManager playerInteractionManager;
        [HideInInspector] public NarrativeDesignManager narrativeDesignManager;
        [HideInInspector] public TriggerUnityEvent onTriggerSent;
        private readonly Queue<GetResponseResponse> _getResponseResponses = new();
        private bool _animationPlaying;
        private Channel _channel;
        private Animator _characterAnimator;
        private ConvaiService.ConvaiServiceClient _client;
        private ConvaiChatUIHandler _convaiChatUIHandler;
        private ConvaiCrosshairHandler _convaiCrosshairHandler;
        private ConvaiGroupNPCController _convaiGroupNPCController;
        private ConvaiPlayerDataSO _convaiPlayerData;
        private TMP_InputField _currentInputField;
        private bool _groupNPCComponentNotFound;
        private ConvaiGRPCAPI _grpcAPI;
        private bool _isActionActive;
        private bool _isLipSyncActive;
        private bool _stopAudioPlayingLoop;
        private bool _stopHandlingInput;
        public ActionConfig ActionConfig;

        private bool IsInConversationWithAnotherNPC
        {
            get
            {
                if (_groupNPCComponentNotFound) return false;
                if (_convaiGroupNPCController == null)
                {
                    if (TryGetComponent(out ConvaiGroupNPCController component))
                        _convaiGroupNPCController = component;
                    else
                        _groupNPCComponentNotFound = true;
                }

                return _convaiGroupNPCController != null && _convaiGroupNPCController.IsInConversationWithAnotherNPC;
            }
        }

        private string SpeakerID
        {
            get
            {
                if (_convaiPlayerData == null) return string.Empty;
                return _convaiPlayerData.SpeakerID;
            }
        }

        public bool IsCharacterTalking
        {
            get => isCharacterTalking;
            private set => isCharacterTalking = value;
        }

        private FaceModel FaceModel => convaiLipSync == null ? FaceModel.OvrModelName : convaiLipSync.faceModel;

        public string GetEndPointURL => GRPC_API_ENDPOINT;

        // Properties with getters and setters
        [field: NonSerialized] public bool IncludeActionsHandler { get; set; }
        [field: NonSerialized] public bool LipSync { get; set; }
        [field: NonSerialized] public bool HeadEyeTracking { get; set; }
        [field: NonSerialized] public bool EyeBlinking { get; set; }
        [field: NonSerialized] public bool NarrativeDesignManager { get; set; }
        [field: NonSerialized] public bool ConvaiGroupNPCController { get; set; }
        [field: NonSerialized] public bool LongTermMemoryController { get; set; }
        [field: NonSerialized] public bool NarrativeDesignKeyController { get; set; }
        [field: NonSerialized] public bool DynamicInfoController { get; set; }

        public ConvaiNPCAudioManager AudioManager { get; private set; }

        private void Awake()
        {
            ConvaiLogger.Info("Initializing ConvaiNPC : " + characterName, ConvaiLogger.LogCategory.Character);
            InitializeComponents();
            ConvaiLogger.Info("ConvaiNPC component initialized", ConvaiLogger.LogCategory.Character);
        }

        private async void Start()
        {
            // Assign the ConvaiGRPCAPI component in the scene
            _grpcAPI = ConvaiGRPCAPI.Instance;
            InvokeRepeating(nameof(ProcessResponse), 0f, 1 / 100f);


            // Check if the platform is Android
#if UNITY_ANDROID
            // Check if the user has not authorized microphone permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                // Request microphone permission from the user
                Permission.RequestUserPermission(Permission.Microphone);
#endif
            // DO NOT EDIT
            // gRPC setup configuration 

            #region GRPC_SETUP

            SslCredentials credentials = new(); // Create SSL credentials for secure communication
            _channel = new Channel(GRPC_API_ENDPOINT, credentials); // Initialize a gRPC channel with the specified endpoint and credentials
            _client = new ConvaiService.ConvaiServiceClient(_channel); // Initialize the gRPC client for the ConvaiService using the channel

            #endregion

            if (initializeSessionID) sessionID = await ConvaiGRPCAPI.InitializeSessionIDAsync(characterName, _client, characterID, sessionID);
            _convaiChatUIHandler = ConvaiChatUIHandler.Instance;
        }

        private void OnEnable()
        {
            AudioManager.OnCharacterTalkingChanged += HandleIsCharacterTalkingAnimation;
            AudioManager.OnAudioTranscriptAvailable += HandleAudioTranscriptAvailable;
            AudioManager.OnCharacterTalkingChanged += SetCharacterTalking;

            ConvaiNPCManager.Instance.OnActiveNPCChanged += HandleActiveNPCChanged;

            if (_convaiChatUIHandler != null) _convaiChatUIHandler.UpdateCharacterList();
        }

        private void OnDisable()
        {
            if (AudioManager != null)
            {
                AudioManager.OnCharacterTalkingChanged -= HandleIsCharacterTalkingAnimation;
                AudioManager.OnAudioTranscriptAvailable -= HandleAudioTranscriptAvailable;
                AudioManager.OnCharacterTalkingChanged -= SetCharacterTalking;
                AudioManager.PurgeExcessLipSyncFrames -= PurgeLipSyncFrames;
            }

            ConvaiNPCManager.Instance.OnActiveNPCChanged -= HandleActiveNPCChanged;

            if (_convaiChatUIHandler != null) _convaiChatUIHandler.UpdateCharacterList();
        }

        /// <summary>
        ///     Unity callback that is invoked when the application is quitting.
        ///     Stops the loop that plays audio in order.
        /// </summary>
        private void OnApplicationQuit()
        {
            AudioManager.StopAudioLoop();
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(characterID)) characterID = characterID.Trim();
            _convaiChatUIHandler = ConvaiChatUIHandler.Instance;
            if (_convaiChatUIHandler != null) _convaiChatUIHandler.UpdateCharacterList();
        }

        public async void TriggerEvent(string triggerName)
        {
            string triggerMessage = "";
            TriggerConfig trigger = new()
            {
                TriggerName = triggerName,
                TriggerMessage = triggerMessage
            };

            // Send the trigger to the server using GRPC
            await ConvaiGRPCAPI.Instance.SendTriggerData(_client, characterID, trigger, this);

            // Invoke the UnityEvent
            onTriggerSent.Invoke(triggerMessage, triggerName);
        }
        
        public async void TriggerSpeech(string triggerMessage)
        {
            string triggerName = "";
            TriggerConfig trigger = new()
            {
                TriggerName = triggerName,
                TriggerMessage = triggerMessage
            };

            // Send the trigger to the server using GRPC
            await ConvaiGRPCAPI.Instance.SendTriggerData(_client, characterID, trigger, this);

            // Invoke the UnityEvent
            onTriggerSent.Invoke(triggerMessage, triggerName);
        }

        private event Action<bool> OnCharacterTalking;

        private void UpdateWaitUntilLipSync(bool value)
        {
            AudioManager.SetWaitForCharacterLipSync(value);
        }

        private void HandleActiveNPCChanged(ConvaiNPC newActiveNPC)
        {
            // If this NPC is no longer the active NPC, interrupt its speech
            if (this != newActiveNPC && !IsInConversationWithAnotherNPC && ConvaiInputManager.Instance.IsTalkKeyHeld) InterruptCharacterSpeech();
        }


        private void InitializeComponents()
        {
            _convaiChatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();
            _convaiCrosshairHandler = FindObjectOfType<ConvaiCrosshairHandler>();
            _characterAnimator = GetComponent<Animator>();
            AudioManager = gameObject.AddComponent<ConvaiNPCAudioManager>();
            narrativeDesignManager = GetComponent<NarrativeDesignManager>();
            ConvaiPlayerDataSO.GetPlayerData(out _convaiPlayerData);
            InitializePlayerInteractionManager();
            InitializeLipSync();
            StartCoroutine(InitializeActionsHandler());
        }


        private IEnumerator InitializeActionsHandler()
        {
            yield return new WaitForSeconds(1);
            actionsHandler = GetComponent<ConvaiActionsHandler>();
            if (actionsHandler != null)
            {
                _isActionActive = true;
                ActionConfig = actionsHandler.ActionConfig;
            }
        }

        private void InitializePlayerInteractionManager()
        {
            playerInteractionManager = gameObject.AddComponent<ConvaiPlayerInteractionManager>();
            playerInteractionManager.Initialize(this, _convaiCrosshairHandler, _convaiChatUIHandler);
        }

        private void InitializeLipSync()
        {
            convaiLipSync = GetComponent<ConvaiLipSync>();
            if (convaiLipSync != null)
            {
                _isLipSyncActive = true;
                convaiLipSync = GetComponent<ConvaiLipSync>();
                convaiLipSync.OnCharacterLipSyncing += UpdateWaitUntilLipSync;
            }
        }

        private void HandleAudioTranscriptAvailable(string transcript)
        {
            if (isCharacterActive) _convaiChatUIHandler.SendCharacterText(characterName, transcript);
        }

        /// <summary>
        ///     Handles the character's talking animation based on whether the character is currently talking.
        /// </summary>
        private void HandleIsCharacterTalkingAnimation(bool isTalking)
        {
            if (isTalking)
            {
                if (!_animationPlaying)
                {
                    _animationPlaying = true;
                    _characterAnimator.SetBool(Talk, true);
                }
            }
            else
            {
                _animationPlaying = false;
                _characterAnimator.SetBool(Talk, false);
            }
        }

        /// <summary>
        ///     Sends message data to the server asynchronously.
        /// </summary>
        /// <param name="text">The message to send.</param>
        public async void SendTextDataAsync(string text)
        {
            try
            {
                await ConvaiGRPCAPI.Instance.SendTextData(_client, text, characterID,
                    _isActionActive, _isLipSyncActive, ActionConfig, FaceModel, SpeakerID);
            }
            catch (Exception ex)
            {
                ConvaiLogger.Error(ex, ConvaiLogger.LogCategory.Character);
                // Handle the exception, e.g., show a message to the user.
            }
        }

        /// <summary>
        ///     Initializes the session in an asynchronous manner and handles the receiving of results from the server.
        ///     Initiates the audio recording process using the gRPC API.
        /// </summary>
        public async void StartListening()
        {
            if (!MicrophoneManager.Instance.HasAnyMicrophoneDevices())
            {
                NotificationSystemHandler.Instance.NotificationRequest(NotificationType.NoMicrophoneDetected);
                return;
            }

            await _grpcAPI.StartRecordAudio(_client, _isActionActive, _isLipSyncActive, RECORDING_FREQUENCY,
                RECORDING_LENGTH, characterID, ActionConfig, FaceModel, SpeakerID);
        }

        /// <summary>
        ///     Stops the ongoing audio recording process.
        /// </summary>
        public void StopListening()
        {
            // Stop the audio recording process using the ConvaiGRPCAPI StopRecordAudio method
            _grpcAPI.StopRecordAudio();
        }

        /// <summary>
        ///     Add response to the GetResponseResponse Queue
        /// </summary>
        /// <param name="response"></param>
        public void EnqueueResponse(GetResponseResponse response)
        {
            if (response?.AudioResponse == null) return;
            //ConvaiLogger.DebugLog($"Adding Response for Processing: {response.AudioResponse.TextData}", ConvaiLogger.LogCategory.LipSync);
            _getResponseResponses.Enqueue(response);
        }

        public void ClearResponseQueue()
        {
            _getResponseResponses.Clear();
        }

        private void PurgeLipSyncFrames()
        {
            if (convaiLipSync == null) return;
            convaiLipSync.PurgeExcessFrames();
        }

        /// <summary>
        ///     Processes a response fetched from a character.
        /// </summary>
        /// <remarks>
        ///     1. Processes audio/message/face data from the response and adds it to _responseAudios.
        ///     2. Identifies actions from the response and parses them for execution.
        /// </remarks>
        private void ProcessResponse()
        {
            // Check if the character is active and should process the response
            if (isCharacterActive || IsInConversationWithAnotherNPC)
                if (_getResponseResponses.Count > 0)
                {
                    GetResponseResponse getResponseResponse = _getResponseResponses.Dequeue();

                    if (getResponseResponse?.AudioResponse != null)
                    {
                        // Check if text data exists in the response
                        if (getResponseResponse.AudioResponse.AudioData.ToByteArray().Length > 46)
                        {
                            // Initialize empty string for text
                            string textDataString = getResponseResponse.AudioResponse.TextData;

                            byte[] byteAudio = getResponseResponse.AudioResponse.AudioData.ToByteArray();

                            AudioClip clip = AudioManager.ProcessByteAudioDataToAudioClip(byteAudio,
                                getResponseResponse.AudioResponse.AudioConfig.SampleRateHertz.ToString());

                            // Add the response audio along with associated data to the list
                            AudioManager.AddResponseAudio(new ConvaiNPCAudioManager.ResponseAudio
                            {
                                AudioClip = clip,
                                AudioTranscript = textDataString,
                                IsFinal = false
                            });
                        }
                        else if (getResponseResponse.AudioResponse.EndOfResponse)
                        {
                            ConvaiLogger.DebugLog("We have received end of response", ConvaiLogger.LogCategory.LipSync);
                            // Handle the case where there is a DebugLog but no audio response
                            AudioManager.AddResponseAudio(new ConvaiNPCAudioManager.ResponseAudio
                            {
                                AudioClip = null,
                                AudioTranscript = null,
                                IsFinal = true
                            });
                        }
                    }
                }
        }

        public int GetAudioResponseCount()
        {
            return AudioManager.GetAudioResponseCount();
        }

        public void StopAllAudioPlayback()
        {
            AudioManager.StopAllAudioPlayback();
            AudioManager.ClearResponseAudioQueue();
        }

        public void ResetCharacterAnimation()
        {
            if (_characterAnimator != null)
                _characterAnimator.SetBool(Talk, false);

            if (convaiLipSync != null)
                convaiLipSync.ConvaiLipSyncApplicationBase.ClearQueue();
        }

        public void SetCharacterTalking(bool isTalking)
        {
            if (IsCharacterTalking != isTalking)
            {
                ConvaiLogger.Info($"Character {characterName} is talking: {isTalking}", ConvaiLogger.LogCategory.Character);
                IsCharacterTalking = isTalking;
                OnCharacterTalking?.Invoke(IsCharacterTalking);
            }
        }

        public void StopLipSync()
        {
            if (convaiLipSync != null) convaiLipSync.StopLipSync();
        }

        public void InterruptCharacterSpeech()
        {
            _grpcAPI.InterruptCharacterSpeech(this);
        }

        public ConvaiService.ConvaiServiceClient GetClient()
        {
            return _client;
        }

        public void UpdateSessionID(string newSessionID)
        {
            sessionID = newSessionID;
        }

        [Serializable]
        public class TriggerUnityEvent : UnityEvent<string, string>
        {
        }
    }
}