using System;
using System.Collections;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Features.LipSync;
using Convai.Scripts.Runtime.LoggerSystem;
using Service;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     This class is responsible for handling out all the tasks related to NPC to NPC conversation for a NPC of a group
    /// </summary>
    public class ConvaiGroupNPCController : MonoBehaviour
    {
        /// <summary>
        ///     Used to set Player GameObject Transform and lip-sync
        /// </summary>
        private void Awake()
        {
            if (playerTransform == null) playerTransform = Camera.main.transform;
            ConvaiNPC = GetComponent<ConvaiNPC>();
            CONVERSATION_DISTANCE_THRESHOLD = conversationDistanceThreshold == 0 ? Mathf.Infinity : conversationDistanceThreshold;
            TryGetComponent(out _lipSync);
        }

        /// <summary>
        ///     Starts coroutine for player vicinity check and subscribe to necessary events
        /// </summary>
        private void Start()
        {
            _npcGroup = NPC2NPCConversationManager.Instance.npcGroups.Find(c => c.BelongToGroup(this));
            otherNPC = _npcGroup.GroupNPC1 == this ? _npcGroup.GroupNPC2 : _npcGroup.GroupNPC1;
            _checkPlayerVicinityCoroutine = StartCoroutine(CheckPlayerVicinity());
            if (TryGetComponent(out ConvaiNPCAudioManager convaiNPCAudio)) convaiNPCAudio.OnAudioTranscriptAvailable += HandleAudioTranscriptAvailable;
        }

        /// <summary>
        ///     Unsubscribes to the events and stops the coroutine
        /// </summary>
        private void OnDestroy()
        {
            if (TryGetComponent(out ConvaiNPCAudioManager convaiNPCAudio)) convaiNPCAudio.OnAudioTranscriptAvailable -= HandleAudioTranscriptAvailable;
            if(_npc2NPCGrpcClient!=null) _npc2NPCGrpcClient.OnTranscriptAvailable -= HandleTranscriptAvailable;
            if (_checkPlayerVicinityCoroutine != null) StopCoroutine(_checkPlayerVicinityCoroutine);
        }

        /// <summary>
        ///     Shows speech bubble and adds the received text to final transcript
        /// </summary>
        /// <param name="transcript"></param>
        private void HandleAudioTranscriptAvailable(string transcript)
        {
            if (IsInConversationWithAnotherNPC)
                ShowSpeechBubble?.Invoke(transcript);
        }

        private void HandleTranscriptAvailable(string transcript, ConvaiGroupNPCController npcController)
        {
            if (npcController != this) return;
            _finalResponseText += transcript;
        }

        /// <summary>
        ///     Attaches the speech bubble to the NPC game-object
        /// </summary>
        public void AttachSpeechBubble()
        {
            if (TryGetComponent(out ConvaiSpeechBubbleController _)) return;
            gameObject.AddComponent<ConvaiSpeechBubbleController>().Initialize(speechBubblePrefab, this);
        }

        /// <summary>
        ///     Destroys the speech bubble game-object
        /// </summary>
        public void DetachSpeechBubble()
        {
            if (TryGetComponent(out ConvaiSpeechBubbleController convaiSpeechBubble)) Destroy(convaiSpeechBubble);
        }

        /// <summary>
        ///     Store the references of the client and subscribe to the necessary events
        /// </summary>
        /// <param name="client"></param>
        public void InitializeNpc2NpcGrpcClient(NPC2NPCGRPCClient client)
        {
            _npc2NPCGrpcClient = client;
            _npc2NPCGrpcClient.OnTranscriptAvailable += HandleTranscriptAvailable;
        }

        /// <summary>
        ///     Every 0.5 seconds updates if player is near or not and fire events according to the state
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckPlayerVicinity()
        {
            bool previousState = false;
            Vector3 previousPlayerPosition = Vector3.zero;
            yield return new WaitForSeconds(0.1f);
            while (true)
            {
                Vector3 currentPlayerPosition = playerTransform.transform.position;

                // Check if the player has moved more than a certain threshold distance
                if (Vector3.Distance(previousPlayerPosition, currentPlayerPosition) > PLAYER_MOVE_THRESHOLD)
                {
                    // Calculate the distance between the NPC and the player
                    float distanceToPlayer = Vector3.Distance(transform.position, currentPlayerPosition);

                    // Check if the player is within the threshold distance
                    bool isPlayerCurrentlyNear = distanceToPlayer <= CONVERSATION_DISTANCE_THRESHOLD;

                    // If the player's current vicinity state is different from the previous state, raise the event
                    if (isPlayerCurrentlyNear != previousState && !ConvaiNPC.isCharacterActive)
                    {
                        OnPlayerVicinityChanged?.Invoke(isPlayerCurrentlyNear, this);
                        previousState = isPlayerCurrentlyNear; // Update the previous state
                        ConvaiLogger.Info($"Player is currently near {ConvaiNPC.characterName}: {isPlayerCurrentlyNear}", ConvaiLogger.LogCategory.Character);
                    }

                    previousPlayerPosition = currentPlayerPosition; // Update the player's previous position
                    // Check every half second
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        ///     Sends the text to the other NPC in the group
        /// </summary>
        /// <param name="message"></param>
        public async void SendTextDataNPC2NPC(string message)
        {
            if (_npc2NPCGrpcClient == null)
            {
                ConvaiLogger.Warn("No GRPC client initialized for this NPC.", ConvaiLogger.LogCategory.Character);
                return;
            }

            try
            {
                CanRelayMessage = false;
                await Task.Delay(500);
                await _npc2NPCGrpcClient.SendTextData(
                    message,
                    ConvaiNPC.characterID,
                    ConvaiNPC.sessionID,
                    _lipSync != null,
                    FaceModel,
                    this);
            }
            catch (Exception ex)
            {
                ConvaiLogger.Warn($"Error sending message data for NPC2NPC: {ex.Message}", ConvaiLogger.LogCategory.Character);
            }
        }

        public void EndOfResponseReceived()
        {
            if (TryGetComponent(out ConvaiNPCAudioManager convaiNPCAudio)) convaiNPCAudio.OnCharacterTalkingChanged += SendFinalTranscriptToOtherNPC;
            ConversationManager.RelayMessage(_finalResponseText, this);
            _finalResponseText = "";
        }

        private void SendFinalTranscriptToOtherNPC(bool isTalking)
        {
            if (IsInConversationWithAnotherNPC)
            {
                if (!isTalking)
                {
                    ConversationManager.SwitchSpeaker(this);
                    if (TryGetComponent(out ConvaiNPCAudioManager convaiNPCAudio)) convaiNPCAudio.OnCharacterTalkingChanged -= SendFinalTranscriptToOtherNPC;
                    HideSpeechBubble?.Invoke();
                }
                else
                {
                    ConvaiLogger.DebugLog($"{ConvaiNPC.characterName} is currently still talking. ", ConvaiLogger.LogCategory.Character);
                }
            }
        }

        public bool IsPlayerNearMe()
        {
            bool result = Vector3.Distance(transform.position, playerTransform.position) < CONVERSATION_DISTANCE_THRESHOLD;
            ConvaiLogger.Info($"Player is near {CharacterName}: {result}", ConvaiLogger.LogCategory.Character);
            return result;
        }

        public bool IsOtherNPCTalking()
        {
            return otherNPC.ConvaiNPC.IsCharacterTalking;
        }

        #region Serialized Fields

        [Tooltip("The prefab for the speech bubble to display above the NPC. [Optional]")] [SerializeField]
        private NPCSpeechBubble speechBubblePrefab;

        [Tooltip("Attach the Main Player Transform here so that distance check can be performed")] [SerializeField]
        private Transform playerTransform;

        // The distance from the NPC to the player when the NPC will start talking
        [Tooltip("The distance from the NPC to the player when the NPC will start talking. Set to 0 to disable this feature. [Optional]")] [SerializeField] [Range(0f, 100f)]
        private float conversationDistanceThreshold = 5.0f;

        #endregion

        #region Private Attributes

        private const float PLAYER_MOVE_THRESHOLD = 0.5f;
        private float CONVERSATION_DISTANCE_THRESHOLD;
        private string _finalResponseText = "";
        private NPC2NPCGRPCClient _npc2NPCGrpcClient;
        private ConvaiLipSync _lipSync;
        private Coroutine _checkPlayerVicinityCoroutine;
        private FaceModel FaceModel => _lipSync == null ? FaceModel.OvrModelName : _lipSync.faceModel;

        #endregion

        #region Events

        public event Action<string> ShowSpeechBubble;
        public event Action HideSpeechBubble;
        public event Action<bool, ConvaiGroupNPCController> OnPlayerVicinityChanged;
        private NPCGroup _npcGroup;
        private ConvaiGroupNPCController otherNPC;

        #endregion

        #region Public Attributes

        public bool CanRelayMessage { get; set; } = true;
        public NPC2NPCConversationManager ConversationManager { get; set; }
        public bool IsInConversationWithAnotherNPC { get; set; }
        public string CharacterName => ConvaiNPC == null ? string.Empty : ConvaiNPC.characterName;
        public string CharacterID => ConvaiNPC == null ? string.Empty : ConvaiNPC.characterID;
        public ConvaiNPC ConvaiNPC { get; private set; }

        #endregion
    }
}