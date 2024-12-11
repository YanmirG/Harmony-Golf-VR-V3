using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.LoggerSystem;
using Newtonsoft.Json;

namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     API client for the Narrative Design API.
    /// </summary>
    public class NarrativeDesignAPI
    {
        private const string BASE_URL = "https://api.convai.com/character/narrative/";
        private readonly HttpClient _httpClient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NarrativeDesignAPI" /> class.
        /// </summary>
        public NarrativeDesignAPI()
        {
            _httpClient = new HttpClient
            {
                // Set a default request timeout if needed
                Timeout = TimeSpan.FromSeconds(30) // Example: 30 seconds
            };

            // Get the API key from the ConvaiAPIKeySetup object
            if (ConvaiAPIKeySetup.GetAPIKey(out string apiKey))
            {
                // Set default request headers here
                _httpClient.DefaultRequestHeaders.Add("CONVAI-API-KEY", apiKey);

                // Set default headers like Accept to expect a JSON response
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        public async Task<string> CreateSectionAsync(string characterId, string objective, string sectionName, string behaviorTreeCode = null, string btConstants = null)
        {
            string endpoint = "create-section";
            HttpContent content = CreateHttpContent(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "objective", objective },
                { "section_name", sectionName },
                { "behavior_tree_code", behaviorTreeCode },
                { "bt_constants", btConstants }
            });
            return await SendPostRequestAsync(endpoint, content);
        }

        public async Task<string> GetSectionAsync(string characterId, string sectionId)
        {
            string endpoint = "get-section";
            HttpContent content = CreateHttpContent(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "section_id", sectionId }
            });
            return await SendPostRequestAsync(endpoint, content);
        }

        /// <summary>
        ///     Get a list of sections for a character.
        /// </summary>
        /// <param name="characterId"> The character ID. </param>
        /// <returns> A JSON string containing the list of sections. </returns>
        public async Task<string> ListSectionsAsync(string characterId)
        {
            string endpoint = "list-sections";
            HttpContent content = CreateHttpContent(new Dictionary<string, object>
            {
                { "character_id", characterId }
            });
            return await SendPostRequestAsync(endpoint, content);
        }

        public async Task<string> CreateTriggerAsync(string characterId, string triggerName, string triggerMessage = null, string destinationSection = null)
        {
            string endpoint = "create-trigger";

            HttpContent content = CreateHttpContent(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "trigger_message", triggerMessage },
                { "destination_section", destinationSection }
            });

            return await SendPostRequestAsync(endpoint, content);
        }

        public async Task<string> GetTriggerAsync(string characterId, string triggerId)
        {
            string endpoint = "get-trigger";
            HttpContent content = CreateHttpContent(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "trigger_id", triggerId }
            });
            return await SendPostRequestAsync(endpoint, content);
        }

        /// <summary>
        ///     Get a list of triggers for a character.
        /// </summary>
        /// <param name="characterId"> The character ID. </param>
        /// <returns> A JSON string containing the list of triggers. </returns>
        public async Task<string> GetTriggerListAsync(string characterId)
        {
            string endpoint = "list-triggers";
            HttpContent content = CreateHttpContent(new Dictionary<string, object>
            {
                { "character_id", characterId }
            });
            return await SendPostRequestAsync(endpoint, content);
        }

        private static HttpContent CreateHttpContent(Dictionary<string, object> data)
        {
            //Dictionary where all values are not null
            Dictionary<string, object> dataToSend =
                data.Where(keyValuePair => keyValuePair.Value != null).ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);

            // Serialize the dictionary to JSON
            string json = JsonConvert.SerializeObject(dataToSend);

            // Convert JSON to HttpContent
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<string> SendPostRequestAsync(string endpoint, HttpContent content)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(BASE_URL + endpoint, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                ConvaiLogger.Exception($"Request to {endpoint} failed: {e.Message}", ConvaiLogger.LogCategory.GRPC);
                return null;
            }
        }
    }
}