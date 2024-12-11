using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.LoggerSystem;
using Newtonsoft.Json;

namespace Convai.Scripts.Editor.Setup.CharacterImporter
{
    public class CharacterPreviewAPILogic
    {
        private const string BASE_URL = "https://api.convai.com/character/";
        private static HttpClient _httpClient;
        private static string _charID;

        public CharacterPreviewAPILogic(string charID)
        {
            _charID = charID;
        }

        public async Task<string> GetCharacterPreview()
        {
            const string endpoint = "get";
            HttpContent content = CreateHttpContent(new Dictionary<string, object>
            {
                { "charID", _charID }
            });
            string getCharacterPreview = await SendPostRequestAsync(endpoint, content);
            if (getCharacterPreview == null) ConvaiLogger.Warn("Character Preview data is null", ConvaiLogger.LogCategory.UI);

            return getCharacterPreview;
        }

        private static HttpContent CreateHttpContent(Dictionary<string, object> data)
        {
            _httpClient = new HttpClient
            {
                // Set a default request timeout if needed
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (ConvaiAPIKeySetup.GetAPIKey(out string apiKey))
            {
                if (apiKey == null) return null;

                // Set default request headers here
                _httpClient.DefaultRequestHeaders.Add("CONVAI-API-KEY", apiKey);

                // Set default headers like Accept to expect a JSON response
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            //Dictionary where all values are not null
            Dictionary<string, object> dataToSend =
                data.Where(keyValuePair => keyValuePair.Value != null).ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            // Serialize the dictionary to JSON
            string json = JsonConvert.SerializeObject(dataToSend);

            // Convert JSON to HttpContent
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static async Task<string> SendPostRequestAsync(string endpoint, HttpContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content), "HTTP content cannot be null.");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(BASE_URL + endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Request to {endpoint} failed with status code {response.StatusCode}. Response: {errorContent}");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                ConvaiLogger.Error($"Network error occurred while sending request to {endpoint}: {e.Message}", ConvaiLogger.LogCategory.Character);
                throw;
            }
            catch (TaskCanceledException e)
            {
                ConvaiLogger.Error($"Request to {endpoint} timed out: {e.Message}", ConvaiLogger.LogCategory.Character);
                throw;
            }
            catch (Exception e)
            {
                ConvaiLogger.Error($"Unexpected error occurred while sending request to {endpoint}: {e.Message}", ConvaiLogger.LogCategory.Character);
                throw;
            }
        }
    }
}