using System.Collections;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.UI;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.CharacterImporter
{
    public class CharacterImporterUI
    {
        private readonly TextField _characterID;
        private readonly VisualElement _root;

        private void RenderModelPreview()
        {
            EditorCoroutineUtility.StartCoroutine(RenderModelPreviewCoroutine(), this);
        }

        private IEnumerator RenderModelPreviewCoroutine()
        {
            Task<string> previewTask = new CharacterPreviewAPILogic(_characterID.text.Trim()).GetCharacterPreview();
            yield return new WaitUntil(() => previewTask.IsCompleted);

            if (string.IsNullOrEmpty(previewTask.Result)) yield break;

            CharacterPreview characterPreview = JsonConvert.DeserializeObject<CharacterPreview>(previewTask.Result);
            if (characterPreview == null) yield break;

            yield return UpdateCharacterThumbnail(characterPreview.model_details?.modelPlaceholder);
            UpdateCharacterInfo(characterPreview);
        }

        private void UpdateCharacterInfo(CharacterPreview preview)
        {
            _root.Q<Label>("character-name").text = preview.character_name;
            _root.Q<Label>("character-id").text = $"Character ID: {preview.character_id}";
            _root.Q<Label>("character-language").text = $"Language: {preview.language_code}";
            _root.Q<Label>("character-backstory").text = preview.backstory;
        }

        private IEnumerator UpdateCharacterThumbnail(string modelLink)
        {
            if (string.IsNullOrEmpty(modelLink)) yield break;

            VisualElement characterThumbnail = _root.Q<VisualElement>("character-thumbnail");
            if (characterThumbnail == null) yield break;

            using UnityWebRequest www = UnityWebRequestTexture.GetTexture(modelLink);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                characterThumbnail.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                ConvaiLogger.Error($"Failed to load image: {www.error}", ConvaiLogger.LogCategory.UI);
            }
        }

#if READY_PLAYER_ME
        public CharacterImporterUI(VisualElement root, ConvaiChatUIHandler convaiChatUIHandler)
        {
            _root = root;
            _characterID = root.Q<TextField>("character-id");
            CharacterImporterLogic characterImporterLogic = new(convaiChatUIHandler);

            SetupButtons(root, characterImporterLogic);
        }

        private void SetupButtons(VisualElement root, CharacterImporterLogic characterImporterLogic)
        {
            root.Q<Button>("create-character-from-playground").clicked += OpenCharacterCreator;
            root.Q<Button>("import-character").clicked += () => ImportCharacter(characterImporterLogic);
            root.Q<Button>("preview-character").clicked += RenderModelPreview;
        }

        private static void OpenCharacterCreator()
        {
            Application.OpenURL("https://docs.convai.com/api-docs/convai-playground/character-creator-tool/create-character");
        }

        private void ImportCharacter(CharacterImporterLogic characterImporterLogic)
        {
            _ = characterImporterLogic.DownloadCharacter(_characterID.text.Trim());
        }
#endif
    }
}