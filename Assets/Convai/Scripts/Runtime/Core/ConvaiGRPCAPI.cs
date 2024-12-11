using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.UI;
using Convai.Scripts.Runtime.Utils;
using Google.Protobuf;
using Grpc.Core;
using Service;
using UnityEngine;
using static Service.GetResponseRequest.Types;

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    ///     This class is dedicated to manage all communications between the Convai server and plugin, in addition to
    ///     processing any data transmitted during these interactions. It abstracts the underlying complexities of the plugin,
    ///     providing a seamless interface for users. Modifications to this class are discouraged as they may impact the
    ///     stability and functionality of the system. This class is maintained by the development team to ensure compatibility
    ///     and performance.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConvaiNPCManager))]
    [AddComponentMenu("Convai/Convai GRPC API")]
    public class ConvaiGRPCAPI : MonoBehaviour
    {
        private static bool _isInitializationErrorThrown;
        public static ConvaiGRPCAPI Instance;
        private static bool _usageLimitNotificationSent;
        private ConvaiNPC _activeConvaiNPC;
        private string _apiKey;
        private CancellationTokenSource _cancellationTokenSource;
        private ConvaiChatUIHandler _chatUIHandler;
        private string _currentTranscript;
        private string _isFinalUserQueryTextBuffer = "";

        private void Awake()
        {
            // Singleton pattern: Ensure only one instance of this script is active.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Load API key from a ScriptableObject in Resources folder.
            ConvaiAPIKeySetup.GetAPIKey(out _apiKey);

            // Find and store a reference to the ConvaiChatUIHandler component in the scene.
            _chatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();
        }

        private void Start()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged += HandleActiveNPCChanged;
            _cancellationTokenSource = new CancellationTokenSource();
            MainThreadDispatcher.CreateInstance();
        }

        private void FixedUpdate()
        {
            if (_chatUIHandler != null && !string.IsNullOrEmpty(_currentTranscript)) _chatUIHandler.SendPlayerText(_currentTranscript);
        }

        private void OnDestroy()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged -= HandleActiveNPCChanged;

            InterruptCharacterSpeech(_activeConvaiNPC);
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                ConvaiLogger.Warn("Exception in OnDestroy: " + ex.Message, ConvaiLogger.LogCategory.Character);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }


        /// <summary>
        ///     Asynchronously initializes a session ID by communicating with a gRPC service and returns the session ID if
        ///     successful.
        /// </summary>
        /// <param name="characterName">The name of the character for which the session is being initialized.</param>
        /// <param name="client">The gRPC service client used to make the call to the server.</param>
        /// <param name="characterID">The unique identifier for the character.</param>
        /// <param name="sessionID">The session ID that may be updated during the initialization process.</param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task result contains the initialized session ID if
        ///     successful, or null if the initialization fails.
        /// </returns>
        public static async Task<string> InitializeSessionIDAsync(string characterName, ConvaiService.ConvaiServiceClient client, string characterID, string sessionID)
        {
            ConvaiLogger.DebugLog("Initializing SessionID for character: " + characterName, ConvaiLogger.LogCategory.Character);
            if (client == null)
            {
                ConvaiLogger.Error("gRPC client is not initialized.", ConvaiLogger.LogCategory.Character);
                return null;
            }

            using AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call = client.GetResponse();
            GetResponseRequest getResponseConfigRequest = new()
            {
                GetResponseConfig = new GetResponseConfig
                {
                    CharacterId = characterID,
                    ApiKey = Instance._apiKey,
                    SessionId = sessionID,
                    AudioConfig = new AudioConfig { DisableAudio = true }
                }
            };

            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
                await call.RequestStream.WriteAsync(new GetResponseRequest
                {
                    GetResponseData = new GetResponseData
                    {
                        TextData = "Repeat the following exactly as it is: [Hii]"
                    }
                });

                await call.RequestStream.CompleteAsync();

                while (await call.ResponseStream.MoveNext())
                {
                    GetResponseResponse result = call.ResponseStream.Current;

                    if (!string.IsNullOrEmpty(result.SessionId))
                    {
                        ConvaiLogger.DebugLog("SessionID Initialization SUCCESS for: " + characterName,
                            ConvaiLogger.LogCategory.Character);
                        return result.SessionId;
                    }
                }

                ConvaiLogger.Exception("SessionID Initialization FAILED for: " + characterName, ConvaiLogger.LogCategory.Character);
            }
            catch (RpcException rpcException)
            {
                switch (rpcException.StatusCode)
                {
                    case StatusCode.Cancelled:
                        ConvaiLogger.Exception(rpcException, ConvaiLogger.LogCategory.Character);
                        break;
                    case StatusCode.Unknown:
                        ConvaiLogger.Error($"Unknown error from server: {rpcException.Status.Detail}", ConvaiLogger.LogCategory.Character);
                        break;
                    case StatusCode.PermissionDenied:
                    {
                        if (NotificationSystemHandler.Instance != null && !_isInitializationErrorThrown)
                        {
                            NotificationSystemHandler.Instance.NotificationRequest(NotificationType.UsageLimitExceeded);
                            _isInitializationErrorThrown = true;
                        }

                        break;
                    }
                    default:
                        throw;
                }
            }
            catch (Exception ex)
            {
                ConvaiLogger.Exception(ex, ConvaiLogger.LogCategory.Character);
            }

            return "-1";
        }


        /// <summary>
        ///     Sends text data to the server and processes the response.
        /// </summary>
        /// <param name="client">The gRPC client used to communicate with the server.</param>
        /// <param name="userText">The text data to send to the server.</param>
        /// <param name="characterID">The ID of the character that is sending the text.</param>
        /// <param name="isActionActive">Indicates whether actions are active.</param>
        /// <param name="isLipSyncActive">Indicates whether lip sync is active.</param>
        /// <param name="actionConfig">The action configuration.</param>
        /// <param name="faceModel">The face model.</param>
        /// <param name="speakerId">Speaker ID of the Player</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendTextData(ConvaiService.ConvaiServiceClient client, string userText, string characterID, bool isActionActive, bool isLipSyncActive,
            ActionConfig actionConfig, FaceModel faceModel, string speakerId)
        {
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call =
                GetAsyncDuplexStreamingCallOptions(client);

            GetResponseRequest getResponseConfigRequest = CreateGetResponseRequest(
                isActionActive,
                isLipSyncActive,
                0,
                characterID,
                actionConfig,
                faceModel,
                speakerId);

            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
                await call.RequestStream.WriteAsync(new GetResponseRequest
                {
                    GetResponseData = new GetResponseData
                    {
                        TextData = userText
                    }
                });
                await call.RequestStream.CompleteAsync();

                // Store the task that receives results from the server.
                Task receiveResultsTask = Task.Run(
                    async () => { await ReceiveResultFromServer(call, _cancellationTokenSource.Token); },
                    _cancellationTokenSource.Token);

                // Await the task if needed to ensure it completes before this method returns [OPTIONAL]
                await receiveResultsTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ConvaiLogger.Error(ex, ConvaiLogger.LogCategory.Character);
            }
        }

        // This method will be called whenever the active NPC changes.
        private void HandleActiveNPCChanged(ConvaiNPC newActiveNPC)
        {
            if (newActiveNPC != null)
                InterruptCharacterSpeech(newActiveNPC);

            // Cancel the ongoing gRPC call
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                ConvaiLogger.Warn("Exception in GRPCAPI:HandleActiveNPCChanged: " + e.Message,
                    ConvaiLogger.LogCategory.Character);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                ConvaiLogger.Info("The Cancellation Token Source was Disposed in GRPCAPI:HandleActiveNPCChanged",
                    ConvaiLogger.LogCategory.Character);
            }

            _cancellationTokenSource = new CancellationTokenSource(); // Create a new token for future calls
            _activeConvaiNPC = newActiveNPC;
        }

        /// <summary>
        ///     Starts recording audio and sends it to the server for processing.
        /// </summary>
        /// <param name="client">gRPC service Client object</param>
        /// <param name="isActionActive">Bool specifying whether we are expecting action responses</param>
        /// <param name="isLipSyncActive"></param>
        /// <param name="recordingFrequency">Frequency of the audio being sent</param>
        /// <param name="recordingLength">Length of the recording from the microphone</param>
        /// <param name="characterID">Character ID obtained from the playground</param>
        /// <param name="actionConfig">Object containing the action configuration</param>
        /// <param name="faceModel"></param>
        /// <param name="speakerID">Speaker ID of the Player</param>
        public async Task StartRecordAudio(ConvaiService.ConvaiServiceClient client, bool isActionActive, bool isLipSyncActive, int recordingFrequency, int recordingLength,
            string characterID, ActionConfig actionConfig, FaceModel faceModel, string speakerID)
        {
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call = GetAsyncDuplexStreamingCallOptions(client);

            GetResponseRequest getResponseConfigRequest =
                CreateGetResponseRequest(isActionActive, isLipSyncActive, recordingFrequency, characterID, actionConfig, faceModel, speakerID);

            ConvaiLogger.DebugLog(getResponseConfigRequest.ToString(), ConvaiLogger.LogCategory.Character);

            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
            }
            catch (Exception ex)
            {
                ConvaiLogger.Error(ex, ConvaiLogger.LogCategory.Character);
                return; // early return on error
            }

            AudioClip audioClip = Microphone.Start(MicrophoneManager.Instance.SelectedMicrophoneName, false, recordingLength, recordingFrequency);

            MicrophoneTestController.Instance.CheckMicrophoneDeviceWorkingStatus(audioClip);

            ConvaiLogger.Info(_activeConvaiNPC.characterName + " is now listening", ConvaiLogger.LogCategory.Character);
            OnPlayerSpeakingChanged?.Invoke(true);

            await ProcessAudioContinuously(call, recordingFrequency, recordingLength, audioClip);
        }

        private AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> GetAsyncDuplexStreamingCallOptions(ConvaiService.ConvaiServiceClient client)
        {
            Metadata headers = new()
            {
                { "source", "Unity" },
                { "version", "3.2.0" }
            };

            CallOptions options = new(headers);
            return client.GetResponse(options);
        }

        /// <summary>
        ///     Creates a GetResponseRequest object configured with the specified parameters for initiating a gRPC call.
        /// </summary>
        /// <param name="isActionActive">Indicates whether actions are enabled for the character.</param>
        /// <param name="isLipSyncActive">Indicates whether lip sync is enabled for the character.</param>
        /// <param name="recordingFrequency">The frequency at which the audio is recorded.</param>
        /// <param name="characterID">The unique identifier for the character.</param>
        /// <param name="actionConfig">The configuration for character actions.</param>
        /// <param name="faceModel">The facial model configuration for the character.</param>
        /// <param name="speakerID"></param>
        /// <param name="npc"></param>
        /// <returns>A GetResponseRequest object configured with the provided settings.</returns>
        private GetResponseRequest CreateGetResponseRequest(bool isActionActive, bool isLipSyncActive, int recordingFrequency, string characterID, ActionConfig actionConfig = null,
            FaceModel faceModel = FaceModel.OvrModelName, string speakerID = "", ConvaiNPC npc = null)
        {
            GetResponseRequest getResponseConfigRequest = new()
            {
                GetResponseConfig = new GetResponseConfig
                {
                    CharacterId = characterID,
                    ApiKey = _apiKey, // Assumes apiKey is available
                    SessionId = npc?.sessionID ?? _activeConvaiNPC?.sessionID ?? "-1", // Assumes _activeConvaiNPC would not be null, else this will throw NullReferenceException
                    SpeakerId = speakerID,
                    AudioConfig = new AudioConfig
                    {
                        SampleRateHertz = recordingFrequency,
                        EnableFacialData = isLipSyncActive,
                        FaceModel = faceModel
                    }
                }
            };

            if (_activeConvaiNPC != null)
            {
                if (_activeConvaiNPC.TryGetComponent(out NarrativeDesignKeyController ndController))
                {
                    foreach (NarrativeDesignKeyController.NarrativeDesignKey templateKey in ndController.narrativeDesignKeys)
                    {
                        getResponseConfigRequest.GetResponseConfig.NarrativeTemplateKeys.Add(templateKey.name, templateKey.value);
                    }
                }
                if (_activeConvaiNPC.TryGetComponent(out DynamicInfoController diController))
                {
                    getResponseConfigRequest.GetResponseConfig.DynamicInfoConfig = diController.DynamicInfoConfig;
                }
            }

            if (isActionActive || _activeConvaiNPC != null) getResponseConfigRequest.GetResponseConfig.ActionConfig = actionConfig;

            return getResponseConfigRequest;
        }

        /// <summary>
        ///     Processes audio data continuously from a microphone input and sends it to the server via a gRPC call.
        /// </summary>
        /// <param name="call">The streaming call to send audio data to the server.</param>
        /// <param name="recordingFrequency">The frequency at which the audio is recorded.</param>
        /// <param name="recordingLength">The length of the audio recording in seconds.</param>
        /// <param name="audioClip">The AudioClip object that contains the audio data from the microphone.</param>
        /// <returns>A task that represents the asynchronous operation of processing and sending audio data.</returns>
        private async Task ProcessAudioContinuously(AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call, int recordingFrequency, int recordingLength,
            AudioClip audioClip)
        {
            // Run the receiving results from the server in the background without awaiting it here.
            Task receiveResultsTask = Task.Run(async () => { await ReceiveResultFromServer(call, _cancellationTokenSource.Token); }, _cancellationTokenSource.Token);

            int pos = 0;
            float[] audioData = new float[recordingFrequency * recordingLength];

            while (Microphone.IsRecording(MicrophoneManager.Instance.SelectedMicrophoneName))
            {
                await Task.Delay(200);
                int newPos = Microphone.GetPosition(MicrophoneManager.Instance.SelectedMicrophoneName);
                int diff = newPos - pos;

                if (diff > 0)
                {
                    if (audioClip == null)
                    {
                        try
                        {
                            _cancellationTokenSource?.Cancel();
                        }
                        catch (Exception e)
                        {
                            // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                            ConvaiLogger.Warn("Exception when Audio Clip is null: " + e.Message,
                                ConvaiLogger.LogCategory.Character);
                        }
                        finally
                        {
                            _cancellationTokenSource?.Dispose();
                            _cancellationTokenSource = null;
                            ConvaiLogger.Info("The Cancellation Token Source was Disposed because the Audio Clip was empty.",
                                ConvaiLogger.LogCategory.Character);
                        }

                        break;
                    }

                    audioClip.GetData(audioData, pos);
                    await ProcessAudioChunk(call, diff, audioData);
                    pos = newPos;
                }
            }

            // Process any remaining audio data.
            await ProcessAudioChunk(call,
                Microphone.GetPosition(MicrophoneManager.Instance.SelectedMicrophoneName) - pos,
                audioData).ConfigureAwait(false);

            await call.RequestStream.CompleteAsync();
        }

        /// <summary>
        ///     Stops recording and processing the audio.
        /// </summary>
        public void StopRecordAudio()
        {
            // End microphone recording
            Microphone.End(MicrophoneManager.Instance.SelectedMicrophoneName);
            _usageLimitNotificationSent = false;

            try
            {
                ConvaiLogger.Info(_activeConvaiNPC.characterName + " has stopped listening", ConvaiLogger.LogCategory.Character);
                OnPlayerSpeakingChanged?.Invoke(false);
            }
            catch (Exception)
            {
                ConvaiLogger.Error("No active NPC found", ConvaiLogger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Processes each audio chunk and sends it to the server.
        /// </summary>
        /// <param name="call">gRPC Streaming call connecting to the getResponse function</param>
        /// <param name="diff">Length of the audio data from the current position to the position of the last sent chunk</param>
        /// <param name="audioData">Chunk of audio data that we want to be processed</param>
        private static async Task ProcessAudioChunk(AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call, int diff, IReadOnlyList<float> audioData)
        {
            if (diff > 0)
            {
                // Convert audio data to byte array
                byte[] audioByteArray = new byte[diff * sizeof(short)];

                for (int i = 0; i < diff; i++)
                {
                    float sample = audioData[i];
                    short shortSample = (short)(sample * short.MaxValue);
                    byte[] shortBytes = BitConverter.GetBytes(shortSample);
                    audioByteArray[i * sizeof(short)] = shortBytes[0];
                    audioByteArray[i * sizeof(short) + 1] = shortBytes[1];
                }

                // Send audio data to the gRPC server
                try
                {
                    await call.RequestStream.WriteAsync(new GetResponseRequest
                    {
                        GetResponseData = new GetResponseData
                        {
                            AudioData = ByteString.CopyFrom(audioByteArray)
                        }
                    });
                }
                catch (RpcException rpcException)
                {
                    switch (rpcException.StatusCode)
                    {
                        case StatusCode.Cancelled:
                            ConvaiLogger.Error(rpcException, ConvaiLogger.LogCategory.Character);
                            break;
                        case StatusCode.PermissionDenied:
                        {
                            if (NotificationSystemHandler.Instance != null && !_usageLimitNotificationSent)
                            {
                                NotificationSystemHandler.Instance.NotificationRequest(NotificationType.UsageLimitExceeded);
                                _usageLimitNotificationSent = true;
                            }

                            break;
                        }
                        default:
                            throw;
                    }
                }
                catch (Exception ex)
                {
                    ConvaiLogger.Error(ex, ConvaiLogger.LogCategory.Character);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="newActiveNPC"></param>
        public void InterruptCharacterSpeech(ConvaiNPC newActiveNPC)
        {
            // If the active NPC is speaking, cancel the ongoing gRPC call,
            // clear the response queue, and reset the character's speaking state, lip-sync, animation, and audio playback
            if (newActiveNPC != null && newActiveNPC.isCharacterActive)
            {
                // Cancel the ongoing gRPC call
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (Exception e)
                {
                    // Handle the Exception, which can occur if the CancellationTokenSource is already disposed. 
                    ConvaiLogger.Warn("Exception in Interrupt Character Speech: " + e.Message, ConvaiLogger.LogCategory.Character);
                }
                finally
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    ConvaiLogger.Info($"The Cancellation Token Source for {newActiveNPC} was Disposed in ConvaiGRPCAPI:InterruptCharacterSpeech.",
                        ConvaiLogger.LogCategory.Character);
                }

                _cancellationTokenSource = new CancellationTokenSource(); // Create a new token for future calls

                CharacterInterrupted?.Invoke();

                // Clear the response queue
                newActiveNPC.ClearResponseQueue();

                // Reset the character's speaking state
                newActiveNPC.SetCharacterTalking(false);

                // Stop any ongoing audio playback
                newActiveNPC.StopAllAudioPlayback();

                // Stop any ongoing lip sync for active NPC
                newActiveNPC.StopLipSync();

                // Reset the character's animation to idle
                newActiveNPC.ResetCharacterAnimation();
            }
        }

        private async Task ReceiveResultFromServer(AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call, CancellationToken cancellationToken,
            ConvaiNPC npc = null)
        {
            Queue<LipSyncBlendFrameData> lipSyncBlendFrameQueue = new();
            bool firstSilFound = false;
            if (npc != null) npc.isCharacterActive = true;
            while (!cancellationToken.IsCancellationRequested && await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                try
                {
                    GetResponseResponse result = call.ResponseStream.Current;
                    OnResultReceived?.Invoke(result);
                    ProcessCharacterEmotion(result, npc);
                    ProcessUserQuery(result);
                    ProcessBtResponse(result, npc);
                    ProcessActionResponse(result, npc);
                    ProcessAudioResponse(result, lipSyncBlendFrameQueue, ref firstSilFound, npc);
                    ProcessDebugLog(result, call, npc);
                    UpdateSessionId(result, npc);
                }
                catch (RpcException rpcException) when (rpcException.StatusCode == StatusCode.Cancelled)
                {
                    ConvaiLogger.Error(rpcException, ConvaiLogger.LogCategory.Character);
                }
                catch (Exception ex)
                {
                    ConvaiLogger.DebugLog(ex, ConvaiLogger.LogCategory.Character);
                }

            if (cancellationToken.IsCancellationRequested)
                await call.RequestStream.CompleteAsync();
        }

        private ConvaiNPC NPCToSendResponse(ConvaiNPC npc)
        {
            return npc ?? _activeConvaiNPC;
        }

        private void ProcessCharacterEmotion(GetResponseResponse result, ConvaiNPC npc)
        {
            ConvaiNPC convaiNPC = NPCToSendResponse(npc);
            if (convaiNPC == null || string.IsNullOrEmpty(result.EmotionResponse)) return;
            ConvaiLogger.DebugLog($"Emotion Response from the server: {result.EmotionResponse}", ConvaiLogger.LogCategory.LipSync);
            List<string> newEmotions = result.EmotionResponse.Split(' ').ToList();
            convaiNPC.convaiLipSync.SetCharacterEmotions(newEmotions);
        }

        private void ProcessUserQuery(GetResponseResponse result)
        {
            if (result.UserQuery != null)
            {
                _currentTranscript = _isFinalUserQueryTextBuffer + result.UserQuery.TextData;
                if (result.UserQuery.IsFinal) _isFinalUserQueryTextBuffer += result.UserQuery.TextData;

                if (result.UserQuery.EndOfResponse) _isFinalUserQueryTextBuffer = "";
            }
            else
            {
                _isFinalUserQueryTextBuffer = "";
                _currentTranscript = null;
            }
        }


        private void ProcessBtResponse(GetResponseResponse result, ConvaiNPC npc)
        {
            if (result.BtResponse != null)
                TriggerNarrativeSection(result, npc);
        }

        private void ProcessActionResponse(GetResponseResponse result, ConvaiNPC npc)
        {
            ConvaiNPC convaiNPC = NPCToSendResponse(npc);
            if (result.ActionResponse != null && convaiNPC.actionsHandler != null)
                convaiNPC.actionsHandler.actionResponseList.Add(result.ActionResponse.Action);
        }

        private void ProcessAudioResponse(GetResponseResponse result, Queue<LipSyncBlendFrameData> lipSyncBlendFrameQueue, ref bool firstSilFound, ConvaiNPC npc)
        {
            ConvaiNPC convaiNPC = NPCToSendResponse(npc);
            if (result.AudioResponse?.AudioData == null) return;

            if (result.AudioResponse.AudioData.ToByteArray().Length > 46)
                ProcessAudioData(result, lipSyncBlendFrameQueue, convaiNPC);

            if (result.AudioResponse.VisemesData != null)
                ProcessVisemesData(result, lipSyncBlendFrameQueue, ref firstSilFound, convaiNPC);

            if (result.AudioResponse.BlendshapesData != null)
                ProcessBlendshapesFrame(result, lipSyncBlendFrameQueue, convaiNPC);
        }

        private void ProcessAudioData(GetResponseResponse result, Queue<LipSyncBlendFrameData> lipSyncBlendFrameQueue, ConvaiNPC npc)
        {
            byte[] wavBytes = result.AudioResponse.AudioData.ToByteArray();
            WavHeaderParser parser = new(wavBytes);

            if (npc.convaiLipSync == null)
            {
                ConvaiLogger.DebugLog($"Enqueuing responses: {result.AudioResponse.TextData}", ConvaiLogger.LogCategory.LipSync);
                npc.EnqueueResponse(result);
            }
            else
            {
                LipSyncBlendFrameData.FrameType frameType = npc.convaiLipSync.faceModel == FaceModel.OvrModelName
                    ? LipSyncBlendFrameData.FrameType.Visemes
                    : LipSyncBlendFrameData.FrameType.Blendshape;

                lipSyncBlendFrameQueue.Enqueue(new LipSyncBlendFrameData((int)(parser.CalculateDurationSeconds() * 30), result, frameType));
            }
        }

        private void ProcessVisemesData(GetResponseResponse result, Queue<LipSyncBlendFrameData> lipSyncBlendFrameQueue, ref bool firstSilFound, ConvaiNPC npc)
        {
            if (npc.convaiLipSync == null) return;

            if (Mathf.Approximately(result.AudioResponse.VisemesData.Visemes.Sil, -2) || result.AudioResponse.EndOfResponse)
            {
                if (firstSilFound) lipSyncBlendFrameQueue.Dequeue().Process(npc);
                firstSilFound = true;
            }
            else
            {
                lipSyncBlendFrameQueue.Peek().Enqueue(result.AudioResponse.VisemesData);
            }
        }

        private void ProcessBlendshapesFrame(GetResponseResponse result, Queue<LipSyncBlendFrameData> lipSyncBlendFrameQueue, ConvaiNPC npc)
        {
            if (npc.convaiLipSync == null) return;

            if (lipSyncBlendFrameQueue.Peek().CanProcess() || result.AudioResponse.EndOfResponse)
            {
                lipSyncBlendFrameQueue.Dequeue().Process(npc);
            }
            else
            {
                lipSyncBlendFrameQueue.Peek().Enqueue(result.AudioResponse.FaceEmotion.ArKitBlendShapes);
                if (lipSyncBlendFrameQueue.Peek().CanPartiallyProcess())
                    lipSyncBlendFrameQueue.Peek().ProcessPartially(npc);
            }
        }

        private void ProcessDebugLog(GetResponseResponse result, AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call, ConvaiNPC npc)
        {
            ConvaiNPC convaiNPC = NPCToSendResponse(npc);
            if (result.AudioResponse == null && result.DebugLog != null)
                convaiNPC.EnqueueResponse(call.ResponseStream.Current);
        }

        private void UpdateSessionId(GetResponseResponse result, ConvaiNPC npc)
        {
            ConvaiNPC convaiNPC = NPCToSendResponse(npc);
            if (convaiNPC.sessionID == "-1")
                convaiNPC.sessionID = result.SessionId;
        }


        /// <summary>
        /// </summary>
        /// <param name="result"></param>
        /// <param name="npc"></param>
        private void TriggerNarrativeSection(GetResponseResponse result, ConvaiNPC npc)
        {
            ConvaiNPC convaiNPC = NPCToSendResponse(npc);
            // Trigger the current section of the narrative design manager in the active NPC
            if (result.BtResponse != null)
            {
                ConvaiLogger.DebugLog($"Narrative Design SectionID: {result.BtResponse.NarrativeSectionId}", ConvaiLogger.LogCategory.Character);
                // Get the NarrativeDesignManager component from the active NPC
                NarrativeDesignManager narrativeDesignManager = convaiNPC.narrativeDesignManager;
                if (narrativeDesignManager != null)
                    MainThreadDispatcher.Instance.RunOnMainThread(() => { narrativeDesignManager.UpdateCurrentSection(result.BtResponse.NarrativeSectionId); });
                else
                    ConvaiLogger.Error("NarrativeDesignManager component not found in the active NPC", ConvaiLogger.LogCategory.Character);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="characterID"></param>
        /// <param name="triggerConfig"></param>
        /// <param name="sendingNPC"></param>
        public async Task SendTriggerData(ConvaiService.ConvaiServiceClient client, string characterID, TriggerConfig triggerConfig, ConvaiNPC sendingNPC = null)
        {
            ConvaiLogger.DebugLog($"Sending trigger data: {triggerConfig.TriggerName}", ConvaiLogger.LogCategory.Character);
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call = GetAsyncDuplexStreamingCallOptions(client);

            GetResponseRequest getResponseConfigRequest = CreateGetResponseRequest(true, true, 0, characterID, npc: sendingNPC);

            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
                await call.RequestStream.WriteAsync(new GetResponseRequest
                {
                    GetResponseData = new GetResponseData
                    {
                        TriggerData = triggerConfig
                    }
                });
                await call.RequestStream.CompleteAsync();

                // Store the task that receives results from the server.
                Task receiveResultsTask = Task.Run(
                    async () => { await ReceiveResultFromServer(call, _cancellationTokenSource.Token, sendingNPC); },
                    _cancellationTokenSource.Token);

                // Await the task if needed to ensure it completes before this method returns [OPTIONAL]
                await receiveResultsTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ConvaiLogger.Error(ex, ConvaiLogger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Asynchronously sends feedback to the server.
        /// </summary>
        /// <param name="thumbsUp">Indicates whether the feedback is a thumbs up or thumbs down.</param>
        /// <param name="interactionID">The ID associated with the interaction.</param>
        /// <param name="feedbackText">The text content of the feedback.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SendFeedback(bool thumbsUp, string interactionID, string feedbackText)
        {
            // Create a FeedbackRequest object with the provided parameters.
            FeedbackRequest request = new()
            {
                InteractionId = interactionID,
                CharacterId = _activeConvaiNPC.characterID,
                SessionId = _activeConvaiNPC.sessionID,
                TextFeedback = new FeedbackRequest.Types.Feedback
                {
                    FeedbackText = feedbackText,
                    ThumbsUp = thumbsUp
                }
            };

            try
            {
                // Send the feedback request asynchronously and await the response.
                FeedbackResponse response = await _activeConvaiNPC.GetClient().SubmitFeedbackAsync(request, cancellationToken: _cancellationTokenSource.Token);

                // Log the feedback response.
                ConvaiLogger.Info(response.FeedbackResponse_, ConvaiLogger.LogCategory.Character);
            }
            catch (RpcException rpcException)
            {
                // Log an exception if there is an error in sending the feedback.
                ConvaiLogger.Exception(rpcException, ConvaiLogger.LogCategory.Character);
            }
        }

        #region Events

        public event Action CharacterInterrupted; // Event to notify when the character's speech is interrupted
        public event Action<GetResponseResponse> OnResultReceived; // Event to notify when a response is received from the server
        public event Action<bool> OnPlayerSpeakingChanged; // Event to notify when the player starts or stops speaking

        #endregion
    }
}