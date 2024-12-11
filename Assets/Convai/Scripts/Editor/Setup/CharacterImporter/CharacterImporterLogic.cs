using Newtonsoft.Json;
#if READY_PLAYER_ME
using ReadyPlayerMe.Core.Editor;
using ReadyPlayerMe.Core;
using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Extensions;
using Convai.Scripts.Runtime.Features.LipSync;
using Convai.Scripts.Runtime.Features.LipSync.Models;
using Convai.Scripts.Runtime.Features.LipSync.Visemes;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEditor;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Convai.Scripts.Runtime.UI;
using UnityEngine;
#endif

namespace Convai.Scripts.Editor.Setup.CharacterImporter
{
    public class CharacterImporterLogic
    {
        #region Nested type: GetRequest

        private class GetRequest
        {
            [JsonProperty("charID")] public string CharID;

            public GetRequest(string charID)
            {
                CharID = charID;
            }
        }

        #endregion

        #region Nested type: GetResponse

        private class GetResponse
        {
            [JsonProperty("backstory")] public string Backstory;
            [JsonProperty("character_actions")] public string[] CharacterActions;
            [JsonProperty("character_emotions")] public string[] CharacterEmotions;
            [JsonProperty("character_id")] public string CharacterID;
            [JsonProperty("character_name")] public string CharacterName;
            [JsonProperty("model_details")] public ModelDetails ModelDetail;
            [JsonProperty("timestamp")] public string Timestamp;
            [JsonProperty("user_id")] public string UserID;
            [JsonProperty("voice_type")] public string VoiceType;

            #region Nested type: ModelDetails

            internal class ModelDetails
            {
                [JsonProperty("modelLink")] public string ModelLink;
                [JsonProperty("modelPlaceholder")] public string ModelPlaceholder;
                [JsonProperty("modelType")] public string ModelType;
            }

            #endregion
        }

        #endregion

#if READY_PLAYER_ME
        /// <summary>
        ///     The color palette used for the character text.
        /// </summary>
        private static readonly Color[] ColorPalette =
        {
            new(1f, 0f, 0f), new(0f, 1f, 0f), new(0f, 0f, 1f),
            new(1f, 1f, 0f), new(0f, 1f, 1f), new(1f, 0f, 1f),
            new(1f, 0.5f, 0f), new(0.5f, 0f, 0.5f), new(0f, 0.5f, 0f),
            new(0.5f, 0.5f, 0.5f), new(1f, 0.8f, 0.6f), new(0.6f, 0.8f, 1f),
            new(0.8f, 0.6f, 1f), new(1f, 0.6f, 0.8f), new(0.7f, 0.4f, 0f),
            new(0f, 0.7f, 0.7f), new(0.7f, 0.7f, 0f), new(0f, 0.7f, 0.4f),
            new(0.7f, 0f, 0.2f), new(0.9f, 0.9f, 0.9f)
        };

        private readonly ConvaiChatUIHandler _chatUIHandler;

        public CharacterImporterLogic(ConvaiChatUIHandler chatUIHandler)
        {
            _chatUIHandler = chatUIHandler;
        }


        /// <summary>
        ///     Downloads the character model and sets up the character in the scene.
        /// </summary>
        /// <param name="characterID"> The character ID.</param>
        public async Task DownloadCharacter(string characterID)
        {
            if (!ConvaiAPIKeySetup.GetAPIKey(out string apiKey)) return;

            try
            {
                EditorUtility.DisplayProgressBar("Connecting", "Collecting resources...", 0f);

                GetResponse getResponseContent = await GetCharacterDataAsync(characterID, apiKey);
                string modelLink = getResponseContent.ModelDetail.ModelLink;
                string characterName = getResponseContent.CharacterName.Trim();


                AvatarObjectLoader avatarLoader = new()
                {
                    AvatarConfig = Resources.Load<AvatarConfig>("ConvaiRPMAvatarConfig")
                };

                DirectoryUtility.DefaultAvatarFolder = $"Convai/Characters/Mesh Data/{characterName}";

                CompletionEventArgs args = await LoadAvatarAsync(avatarLoader, modelLink, characterName);

                AvatarLoaderSettings avatarLoaderSettings = Resources.Load<AvatarLoaderSettings>("ConvaiAvatarLoaderSettings");
                string path =
                    $"{DirectoryUtility.GetRelativeProjectPath(args.Avatar.name, AvatarCache.GetAvatarConfigurationHash(avatarLoaderSettings.AvatarConfig))}/{args.Avatar.name}";
                GameObject avatar = PrefabHelper.CreateAvatarPrefab(args.Metadata, path, avatarConfig: avatarLoaderSettings.AvatarConfig);

                SetupCharacter(characterID, characterName, avatar, args);

                ConvaiLogger.DebugLog($"Character '{characterName}' downloaded and set up successfully.", ConvaiLogger.LogCategory.Character);
            }
            catch (WebException e)
            {
                EditorUtility.ClearProgressBar();
                ConvaiLogger.Error(e.Message + "\nPlease check if Character ID is correct.", ConvaiLogger.LogCategory.Character);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                ConvaiLogger.Error(e.Message, ConvaiLogger.LogCategory.Character);
            }
        }

        private async Task<GetResponse> GetCharacterDataAsync(string characterID, string apiKey)
        {
            GetRequest getRequest = new(characterID);
            string stringGetRequest = JsonConvert.SerializeObject(getRequest);

            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("CONVAI-API-KEY", apiKey);

            HttpResponseMessage response = await client.PostAsync(
                "https://api.convai.com/character/get",
                new StringContent(stringGetRequest, Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GetResponse>(responseContent);
        }

        private async Task<CompletionEventArgs> LoadAvatarAsync(AvatarObjectLoader avatarLoader, string modelLink, string characterName)
        {
            TaskCompletionSource<CompletionEventArgs> tcs = new();

            avatarLoader.OnProgressChanged += (_, progressArgs) =>
                EditorUtility.DisplayProgressBar("Downloading Character", $"Downloading character model {characterName}: {progressArgs.Progress * 100f}%",
                    progressArgs.Progress);

            avatarLoader.OnCompleted += (_, args) =>
            {
                EditorUtility.ClearProgressBar();
                tcs.SetResult(args);
            };

            avatarLoader.OnFailed += (_, error) =>
            {
                EditorUtility.ClearProgressBar();
                ConvaiLogger.Error($"Failed to download character: {error}", ConvaiLogger.LogCategory.Character);
                tcs.SetException(new Exception($"Failed to download character: {error}"));
            };

            avatarLoader.LoadAvatar(modelLink);

            return await tcs.Task;
        }

        /// <summary>
        ///     Sets up the character in the scene with the downloaded character model.
        /// </summary>
        /// <param name="characterID"> The character ID.</param>
        /// <param name="characterName"> The name of the character.</param>
        /// <param name="avatar"> The avatar GameObject to set up.</param>
        /// <param name="args"> The completion event arguments.</param>
        private void SetupCharacter(string characterID, string characterName, GameObject avatar, CompletionEventArgs args)
        {
            SetupCharacterMetadata(characterName, avatar);
            SetupCollision(avatar);
            avatar.AddComponent<AudioSource>();
            SetupAnimator(args, avatar);
            ConvaiNPC convaiNPCComponent = SetupConvaiComponents(characterID, characterName, avatar);
            SetupLipsync(avatar);
            SetupChatUI(convaiNPCComponent);

            PrefabUtility.SaveAsPrefabAsset(avatar, $"Assets/Convai/Characters/Prefabs/{avatar.name}.prefab");
            Object.DestroyImmediate(args.Avatar, true);
            Selection.activeObject = avatar;
        }


        /// <summary>Setups the lipsync.</summary>
        /// <param name="avatar">The avatar.</param>
        private static void SetupLipsync(GameObject avatar)
        {
            ConvaiLipSync convaiLipSync = avatar.AddComponent<ConvaiLipSync>();
            if (convaiLipSync == null) throw new CompositionException("There should be lipsync attached by now in the imported RPM character, something is not right");

            VisemeEffectorsList arkitHead = Resources.Load<VisemeEffectorsList>("RPM/OVR/OVRHeadEffector");
            if (arkitHead == null) throw new ArgumentException("ARKit/ARKitHeadEffector [VisemeEffectorsList] is either deleted somehow or moved to a new location");
            convaiLipSync.FacialExpressionData.Head = new SkinMeshRendererData
            {
                VisemeEffectorsList = arkitHead,
                Renderer = avatar.transform.GetComponentOnChildWithMatchingRegex<SkinnedMeshRenderer>("[Rr]enderer[_ ]?[Hh]ead")
            };

            VisemeEffectorsList arkitTeeth = Resources.Load<VisemeEffectorsList>("RPM/OVR/OVRTeethEffector");
            if (arkitTeeth == null) throw new ArgumentException("ARKit/ARKitHeadEffector [VisemeEffectorsList] is either deleted somehow or moved to a new location");
            convaiLipSync.FacialExpressionData.Teeth = new SkinMeshRendererData
            {
                VisemeEffectorsList = arkitTeeth,
                Renderer = avatar.transform.GetComponentOnChildWithMatchingRegex<SkinnedMeshRenderer>("[Rr]enderer[_ ]?[Tt]eeth")
            };
        }

        /// <summary>
        ///     Sets up the animator for the character.
        /// </summary>
        /// <param name="args">The completion event arguments.</param>
        /// <param name="avatar">The avatar GameObject.</param>
        private static void SetupAnimator(CompletionEventArgs args, GameObject avatar)
        {
            AvatarAnimationHelper.SetupAnimator(args.Metadata, avatar);
            Animator animator = avatar.GetComponent<Animator>();
            // Determine avatar type based on Avatar field in Animator component
            bool isMasculine = animator.avatar.name.Contains("Masculine");

            // Set the appropriate animator controller
            string animatorPath = isMasculine ? "Masculine NPC Animator" : "Feminine NPC Animator";
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(animatorPath);
        }


        /// <summary>
        ///     Sets up the metadata for the character.
        /// </summary>
        /// <param name="characterName">The name of the character.</param>
        /// <param name="avatar">The avatar GameObject.</param>
        private static void SetupCharacterMetadata(string characterName, GameObject avatar)
        {
            avatar.tag = "Character";
            avatar.name = $"Convai NPC {characterName}";
        }

        /// <summary>
        ///     Sets up the collision for the character.
        /// </summary>
        /// <param name="avatar">The avatar GameObject.</param>
        private static void SetupCollision(GameObject avatar)
        {
            CapsuleCollider capsuleColliderComponent = avatar.AddComponent<CapsuleCollider>();
            capsuleColliderComponent.center = new Vector3(0, 0.9f, 0);
            capsuleColliderComponent.radius = 0.3f;
            capsuleColliderComponent.height = 1.8f;
            capsuleColliderComponent.isTrigger = true;
        }

        /// <summary>
        ///     Sets up the chat UI for the character.
        /// </summary>
        /// <param name="convaiNPCComponent">The ConvaiNPC component.</param>
        private void SetupChatUI(ConvaiNPC convaiNPCComponent)
        {
            if (_chatUIHandler != null && convaiNPCComponent.characterName != null &&
                !_chatUIHandler.HasCharacter(convaiNPCComponent.characterName))
            {
                Character newCharacter = new()
                {
                    characterGameObject = convaiNPCComponent,
                    characterName = convaiNPCComponent.characterName,
                    CharacterTextColor = GetRandomColor()
                };
                _chatUIHandler.AddCharacter(newCharacter);
                EditorUtility.SetDirty(_chatUIHandler);
            }
        }

        /// <summary>
        ///     Sets up the Convai components for the character.
        /// </summary>
        /// <param name="characterID">The character ID.</param>
        /// <param name="characterName">The name of the character.</param>
        /// <param name="avatar">The avatar GameObject.</param>
        /// <returns>The ConvaiNPC component.</returns>
        private static ConvaiNPC SetupConvaiComponents(string characterID, string characterName, GameObject avatar)
        {
            ConvaiNPC convaiNPCComponent = avatar.AddComponent<ConvaiNPC>();
            convaiNPCComponent.sessionID = "-1";
            convaiNPCComponent.characterID = characterID;
            convaiNPCComponent.characterName = characterName;

            avatar.AddComponent<ConvaiHeadTracking>();


            return convaiNPCComponent;
        }

        /// <summary>
        ///     Returns a random color from the predefined palette.
        /// </summary>
        /// <returns>A random color from the predefined palette.</returns>
        private static Color GetRandomColor()
        {
            return ColorPalette[Random.Range(0, ColorPalette.Length)];
        }
#endif
    }
}