using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.LoggerSystem;
using Grpc.Core;
using Service;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     Manages the conversation between two Convai powered NPC groups
    /// </summary>
    public class NPC2NPCConversationManager : MonoBehaviour
    {
        private const string GRPC_API_ENDPOINT = "stream.convai.com";
        public static NPC2NPCConversationManager Instance;
        public List<NPCGroup> npcGroups;
        private readonly List<NPCGroup> _groupsWhereConversationEnded = new();
        private string _apiKey = string.Empty;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            LoadApiKey();
        }

        private void Start()
        {
            foreach (NPCGroup group in npcGroups)
                group.Initialize(HandlePlayerVicinityChanged);

            StartConversationWithAllNPCs();
        }

        /// <summary>
        ///     Handles the event of player vicinity change.
        /// </summary>
        /// <param name="isPlayerNear">Indicates if the player is near.</param>
        /// <param name="npc">The NPC for which the vicinity changed.</param>
        private void HandlePlayerVicinityChanged(bool isPlayerNear, ConvaiGroupNPCController npc)
        {
            if (isPlayerNear)
                ResumeConversation(npc);
        }


        /// <summary>
        ///     Processes the message from the sender NPC.
        /// </summary>
        /// <param name="sender">The NPC sending the message.</param>
        /// <param name="topic">The topic of the conversation.</param>
        /// <param name="message">The message to be processed.</param>
        /// <returns>The processed message.</returns>
        private string ProcessMessage(ConvaiGroupNPCController sender, string topic, string message)
        {
            string processedMessage = $"{sender.CharacterName} said \"{message}\" to you. Reply to it. ";

            processedMessage += Random.Range(0, 2) == 0
                ? $"Talk about something related to {message}. "
                : $"Talk about something other than \"{message}\" but related to {topic}. Gently change the conversation topic. ";

            return processedMessage + "Definitely, reply to the message. Dont address speaker. Keep the reply short. Do not repeat the same message, or keep asking same question.";
        }

        /// <summary>
        ///     Relays the message from the sender NPC.
        /// </summary>
        /// <param name="message">The message to be relayed.</param>
        /// <param name="sender">The NPC sending the message.</param>
        /// <param name="performSwitch"></param>
        public void RelayMessage(string message, ConvaiGroupNPCController sender)
        {
            NPCGroup npcGroup = npcGroups.Find(c => c.BelongToGroup(sender));
            if (npcGroup == null)
            {
                ConvaiLogger.Warn("Conversation not found for the sender.", ConvaiLogger.LogCategory.Character);
                return;
            }

            npcGroup.messageToRelay = message;
            if (!npcGroup.CurrentSpeaker.IsPlayerNearMe()) return;
            StartCoroutine(RelayMessageCoroutine(message, sender, npcGroup));
        }

        /// <summary>
        ///     Coroutine to relay the message from the sender NPC.
        /// </summary>
        /// <param name="message">The message to be relayed.</param>
        /// <param name="npcGroup"> The NPC group to relay the message to. </param>
        /// <returns>An IEnumerator to be used in a coroutine.</returns>
        private IEnumerator RelayMessageCoroutine(string message, ConvaiGroupNPCController sender, NPCGroup npcGroup)
        {
            yield return new WaitForSeconds(0.5f);
            ConvaiGroupNPCController receiver = npcGroup.GroupNPC1 == sender ? npcGroup.GroupNPC2 : npcGroup.GroupNPC1;
            while (receiver.ConvaiNPC.IsCharacterTalking)
            {
                yield return new WaitForSeconds(0.1f);
            }
            try
            {
                ConvaiLogger.DebugLog($"Relaying message from {sender.CharacterName} to {receiver.CharacterName}: {message}", ConvaiLogger.LogCategory.Character);

                string processedMessage = ProcessMessage(receiver, npcGroup.topic, message);
                receiver.SendTextDataNPC2NPC(processedMessage);
            }
            catch (Exception e)
            {
                ConvaiLogger.Warn($"Failed to relay message: {e.Message}", ConvaiLogger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Switches the speaker in the conversation.
        /// </summary>
        /// <param name="currentSpeaker">The current speaker NPC.</param>
        /// <returns>The new speaker NPC.</returns>
        public void SwitchSpeaker(ConvaiGroupNPCController currentSpeaker)
        {
            NPCGroup group = npcGroups.Find(g => g.CurrentSpeaker == currentSpeaker);
            if (group != null)
            {
                group.CurrentSpeaker = currentSpeaker == group.GroupNPC1 ? group.GroupNPC2 : group.GroupNPC1;
                ConvaiLogger.DebugLog($"Switching NPC2NPC Speaker to {group.CurrentSpeaker}", ConvaiLogger.LogCategory.Character);
                return;
            }

            ConvaiLogger.Warn("Failed to switch speaker. Current speaker not found in any group.", ConvaiLogger.LogCategory.Character);
        }

        private void LoadApiKey()
        {
            ConvaiAPIKeySetup.GetAPIKey(out _apiKey);
        }

        /// <summary>
        ///     Initializes a single NPC for conversation.
        /// </summary>
        /// <param name="npc">The NPC to initialize.</param>
        /// <param name="grpcClient">The GRPC client to use for the NPC.</param>
        private void InitializeNPC(ConvaiGroupNPCController npc, NPC2NPCGRPCClient grpcClient)
        {
            if (npc == null)
            {
                ConvaiLogger.Warn("The given NPC is null.", ConvaiLogger.LogCategory.Character);
                return;
            }

            npc.ConversationManager = this;
            npc.InitializeNpc2NpcGrpcClient(grpcClient);
            npc.AttachSpeechBubble();
            npc.IsInConversationWithAnotherNPC = true;
            npc.ConvaiNPC.isCharacterActive = false;
        }

        /// <summary>
        ///     Starts the conversation for the given NPC.
        /// </summary>
        /// <param name="npcGroup">The NPC to start the conversation for.</param>
        private void InitializeNPCGroup(NPCGroup npcGroup)
        {
            if (npcGroup == null)
            {
                ConvaiLogger.Warn("The given NPC is not part of any group.", ConvaiLogger.LogCategory.Character);
                return;
            }

            ConvaiGroupNPCController npc1 = npcGroup.GroupNPC1;
            ConvaiGroupNPCController npc2 = npcGroup.GroupNPC2;

            if (npc1.IsInConversationWithAnotherNPC || npc2.IsInConversationWithAnotherNPC)
            {
                ConvaiLogger.Warn($"{npc1.CharacterName} or {npc2.CharacterName} is already in a conversation.", ConvaiLogger.LogCategory.Character);
                return;
            }

            NPC2NPCGRPCClient grpcClient = CreateAndInitializeGRPCClient(npcGroup);

            // Initialize both NPCs
            InitializeNPC(npc1, grpcClient);
            InitializeNPC(npc2, grpcClient);

            npcGroup.CurrentSpeaker = Random.Range(0, 10) % 2 == 0 ? npc1 : npc2;
        }

        /// <summary>
        ///     Creates and initializes a new GRPC client for the given NPC group.
        /// </summary>
        /// <param name="group">The NPC group to create the GRPC client for.</param>
        /// <returns>The initialized GRPC client.</returns>
        private NPC2NPCGRPCClient CreateAndInitializeGRPCClient(NPCGroup group)
        {
            GameObject grpcClientGameObject = new($"GRPCClient_{group.GroupNPC1.CharacterID}_{group.GroupNPC2.CharacterID}")
            {
                transform = { parent = transform }
            };

            NPC2NPCGRPCClient grpcClient = grpcClientGameObject.AddComponent<NPC2NPCGRPCClient>();
            ConvaiService.ConvaiServiceClient serviceClient = CreateNewConvaiServiceClient();
            grpcClient.Initialize(_apiKey, serviceClient, group);
            return grpcClient;
        }

        /// <summary>
        ///     Creates a new ConvaiServiceClient.
        /// </summary>
        /// <returns> The new ConvaiServiceClient. </returns>
        private ConvaiService.ConvaiServiceClient CreateNewConvaiServiceClient()
        {
            try
            {
                SslCredentials credentials = new();
                Channel channel = new(GRPC_API_ENDPOINT, credentials);
                return new ConvaiService.ConvaiServiceClient(channel);
            }
            catch (Exception ex)
            {
                ConvaiLogger.Error($"Failed to create ConvaiServiceClient: {ex.Message}", ConvaiLogger.LogCategory.Character);
                throw;
            }
        }

        /// <summary>
        ///     Resumes the conversation for the given NPC.
        /// </summary>
        /// <param name="sender"> The NPC to resume the conversation for. </param>
        private void ResumeConversation(ConvaiGroupNPCController sender)
        {
            NPCGroup npcGroup = npcGroups.Find(g => g.BelongToGroup(sender));
            if (npcGroup.IsAnyoneTalking()) return;

            if (_groupsWhereConversationEnded.Contains(npcGroup))
            {
                InitializeNPCGroup(npcGroup);
                _groupsWhereConversationEnded.Remove(npcGroup);
            }

            if (string.IsNullOrEmpty(npcGroup.messageToRelay))
            {
                string message = $"Talk about {npcGroup.topic}.";
                npcGroup.CurrentSpeaker.SendTextDataNPC2NPC(message);
                npcGroup.messageToRelay = message;
                ConvaiLogger.DebugLog($"Starting conversation for the first time between {npcGroup.GroupNPC1.CharacterName} and {npcGroup.GroupNPC2.CharacterName}",
                    ConvaiLogger.LogCategory.Character);
            }
            else
            {
                RelayMessage(npcGroup.messageToRelay, npcGroup.CurrentSpeaker);
                ConvaiLogger.DebugLog($"Resuming conversation between {npcGroup.GroupNPC1.CharacterName} and {npcGroup.GroupNPC2.CharacterName}",
                    ConvaiLogger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Ends the conversation for the given NPC.
        /// </summary>
        /// <param name="npc"> The NPC to end the conversation for. </param>
        public void EndConversation(ConvaiGroupNPCController npc)
        {
            NPCGroup group = npcGroups.Find(g => g.BelongToGroup(npc));
            ConvaiLogger.DebugLog($"Ending conversation between {group.GroupNPC1.CharacterName} and {group.GroupNPC2.CharacterName}", ConvaiLogger.LogCategory.Character);

            void EndConversationForNPC(ConvaiGroupNPCController groupNPC)
            {
                groupNPC.IsInConversationWithAnotherNPC = false;
                groupNPC.ConvaiNPC.InterruptCharacterSpeech();
                groupNPC.GetComponent<ConvaiGroupNPCController>().DetachSpeechBubble();
            }

            EndConversationForNPC(group.GroupNPC1);
            EndConversationForNPC(group.GroupNPC2);
            _groupsWhereConversationEnded.Add(group);
            ConvaiNPCManager.Instance.SetActiveConvaiNPC(npc.ConvaiNPC);

            Destroy(transform.Find($"GRPCClient_{group.GroupNPC1.CharacterID}_{group.GroupNPC2.CharacterID}").gameObject);
        }


        /// <summary>
        ///     Starts the conversation with all NPCs.
        /// </summary>
        private void StartConversationWithAllNPCs()
        {
            IEnumerable filteredList = npcGroups
                .Where(npcGroup => npcGroup.BothNPCAreNotNull())
                .Where(npcGroup => npcGroup.BothNPCAreNotActiveNPC());

            foreach (NPCGroup npcGroup in filteredList)
                InitializeNPCGroup(npcGroup);
        }
    }
}