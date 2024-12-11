using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.LoggerSystem;
using Newtonsoft.Json;

namespace Convai.Scripts.Editor.Setup.AccountsSection
{
    public class UserAPIUsage
    {
        private const string BASE_URL = "https://api.convai.com/user/";
        private static HttpClient _httpClient;

        public static async Task<string> GetUserAPIUsage()
        {
            const string endpoint = "user-api-usage";
            HttpContent content = CreateHttpContent(new Dictionary<string, object>());
            string userAPIUsage = await SendPostRequestAsync(endpoint, content);
            if (userAPIUsage == null)
            {
                ConvaiLogger.Warn("User API Usage is null", ConvaiLogger.LogCategory.UI);

                // return a dummy json string to avoid null reference exception
                return
                    "{\"usage\":{\"planName\":\"Invalid API Key\",\"expiryTs\":\"2022-12-31T23:59:59Z\",\"dailyLimit\":0,\"dailyUsage\":0,\"monthlyLimit\":0,\"monthlyUsage\":0}}";
            }

            return userAPIUsage;
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
                if (apiKey == null)
                {
                    ConvaiLogger.Warn("API Key is null", ConvaiLogger.LogCategory.GRPC);
                    return null;
                }

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
            if (content == null)
            {
                ConvaiLogger.Warn("Content is null", ConvaiLogger.LogCategory.UI);
                return null;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(BASE_URL + endpoint, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                ConvaiLogger.Warn($"Request to {endpoint} failed: {e.Message}", ConvaiLogger.LogCategory.UI);
                return null;
            }
        }
    }
}