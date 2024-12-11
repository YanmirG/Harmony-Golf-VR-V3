using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Assets.Convai.Scripts.Runtime.PlayerStats.API.Model;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.PlayerStats.API.Model;
using Newtonsoft.Json;

namespace Convai.Scripts.Runtime.PlayerStats.API {
    public static class LongTermMemoryAPI {
        private const string BETA_SUBDOMAIN = "beta";
        private const string PROD_SUBDOMAIN = "api";
        private const string BASE_URL = "https://{0}.convai.com/";

        private const bool IS_ON_PROD = false;

        /// <summary>
        ///     Sends a request to the Convai API to create a new speaker ID
        /// </summary>
        /// <param name="apiKey">API Key of the user</param>
        /// <param name="playerName">Player Name which will be used to create Speaker ID</param>
        /// <returns></returns>
        public static async Task<string> CreateNewSpeakerID ( string apiKey, string playerName, Action onFail = null ) {
            if ( string.IsNullOrEmpty( apiKey ) )
                return string.Empty;
            HttpClient client = CreateHttpClient( apiKey );
            HttpContent content = CreateHttpContent( new Dictionary<string, object>
            {
                { "name", playerName }
            } );
            string endPoint = GetEndPoint( NEW_SPEAKER );
            string response = await SendPostRequestAsync( endPoint, client, content );
            return ExtractSpeakerIDFromResponse( response, onFail );
        }

        /// <summary>
        ///     Sends a request to the Convai API to update the status of Long Term Memory for that character
        /// </summary>
        /// <param name="apiKey">API Key of the user</param>
        /// <param name="charID">Character ID of the Convai NPC</param>
        /// <param name="isEnabled">Status of LTM</param>
        /// <returns></returns>
        public static async Task<bool> ToggleLTM ( string apiKey, string charID, bool isEnabled, Action onFail = null ) {
            if ( string.IsNullOrEmpty( apiKey ) )
                return false;
            HttpClient client = CreateHttpClient( apiKey );
            CharacterUpdateRequest request = new( charID, isEnabled );
            string serializeObject = JsonConvert.SerializeObject( request );
            HttpContent content = CreateHttpContent( serializeObject );
            string endPoint = GetEndPoint( CHARACTER_UPDATE );
            string response = await SendPostRequestAsync( endPoint, client, content );
            return ExtractToggleLTMResult( response, onFail );
        }

        /// <summary>
        ///     Sends a request to get the status of Long Term Memory for that character
        /// </summary>
        /// <param name="apiKey">API Key of the user</param>
        /// <param name="charID">Character ID of the Convai NPC</param>
        /// <param name="onFail">Action which will be invoked when web requests fails</param>
        /// <returns></returns>
        public static async Task<bool> GetLTMStatus ( string apiKey, string charID, Action onFail = null ) {
            if ( string.IsNullOrEmpty( apiKey ) )
                return false;
            HttpClient client = CreateHttpClient( apiKey );
            HttpContent content = CreateHttpContent( new Dictionary<string, object>
            {
                { "charID", charID }
            } );
            string endPoint = GetEndPoint( CHARACTER_GET );
            string response = await SendPostRequestAsync( endPoint, client, content );
            return ExtractLTMStatusResult( response, onFail );
        }

        /// <summary>
        ///     Gets List of Speaker ID(s) associated with the given API Key
        /// </summary>
        /// <param name="apiKey">API Key of the Invoker</param>
        /// <param name="onFail">Action to be invoked in case of failure or exception</param>
        /// <returns>List of Speaker ID Details</returns>
        public static async Task<List<SpeakerIDDetails>> GetSpeakerIDList ( string apiKey, Action onFail = null ) {
            if ( string.IsNullOrEmpty( apiKey ) )
                return null;
            HttpClient client = CreateHttpClient( apiKey );
            HttpContent content = CreateHttpContent( string.Empty );
            string endPoint = GetEndPoint( SPEAKER_ID_LIST );
            string response = await SendPostRequestAsync( endPoint, client, content );
            return ExtractSpeakerIDList( response, onFail );
        }
        /// <summary>
        ///     Delete the Speaker ID for the Given API Key
        /// </summary>
        /// <param name="apiKey">API Key of the Invoker</param>
        /// <param name="speakerID">Speaker ID to be Deleted</param>
        /// <param name="onFail">Action to be invoked in case of failure or exception</param>
        /// <returns>True, if ID is deleted successfully, otherwise false</returns>
        public static async Task<bool> DeleteSpeakerID(string apiKey, string speakerID, Action onFail = null ) {
            if ( string.IsNullOrEmpty( apiKey ) )
                return false;
            HttpClient client = CreateHttpClient( apiKey );
            HttpContent content = CreateHttpContent( new Dictionary<string, object> {
                {"speakerId", speakerID }
            } );
            string endPoint = GetEndPoint( DELETE_SPEAKER_ID );
            string response = await SendPostRequestAsync( endPoint, client, content );
            if(string.IsNullOrEmpty( response ) ) {
                onFail?.Invoke();
                return false;
            }
            return true;
        }

        #region Extraction
        private static bool ExtractLTMStatusResult ( string response, Action onFail = null ) {
            try {
                CharacterGetResponse characterGetResponse = JsonConvert.DeserializeObject<CharacterGetResponse>( response );
                return characterGetResponse.MemorySettings.IsEnabled;
            }
            catch ( Exception exception ) {
                ConvaiLogger.Error( $"Exception caught: {exception.Message}", ConvaiLogger.LogCategory.GRPC );
                onFail?.Invoke();
                return false;
            }
        }

        private static bool ExtractToggleLTMResult ( string response, Action onFail = null ) {
            try {
                ServerRequestResponse serverRequestResponse = JsonConvert.DeserializeObject<ServerRequestResponse>( response );
                return serverRequestResponse.Status == "SUCCESS";
            }
            catch ( Exception exception ) {
                ConvaiLogger.Exception( $"Exception caught: {exception.Message}", ConvaiLogger.LogCategory.GRPC );
                onFail?.Invoke();
                return false;
            }
        }

        private static string ExtractSpeakerIDFromResponse ( string response, Action onFail = null ) {
            try {
                ServerRequestResponse serverRequestResponse = JsonConvert.DeserializeObject<ServerRequestResponse>( response );
                return serverRequestResponse.SpeakerID;
            }
            catch ( Exception exception ) {
                ConvaiLogger.Exception( $"Exception caught: {exception.Message}", ConvaiLogger.LogCategory.GRPC );
                onFail?.Invoke();
                return string.Empty;
            }
        }

        private static List<SpeakerIDDetails> ExtractSpeakerIDList ( string response, Action onFail = null ) {
            try {
                List<SpeakerIDDetails> speakers = JsonConvert.DeserializeObject<List<SpeakerIDDetails>>( response );
                return speakers;
            }
            catch ( Exception exception ) {
                ConvaiLogger.Exception( $"Exception caught: {exception.Message}", ConvaiLogger.LogCategory.GRPC );
                onFail?.Invoke();
                return null;
            }
        }
        #endregion
        #region HTTP Request Creation
        private static string GetEndPoint ( string api ) {
            return string.Format( BASE_URL, IS_ON_PROD ? PROD_SUBDOMAIN : BETA_SUBDOMAIN ) + api;
        }

        private static HttpClient CreateHttpClient ( string apiKey ) {
            if ( string.IsNullOrEmpty( apiKey ) )
                return new HttpClient();
            HttpClient httpClient = new() {
                Timeout = TimeSpan.FromSeconds( 30 ),
                DefaultRequestHeaders =
                {
                    Accept =
                    {
                        new MediaTypeWithQualityHeaderValue("application/json")
                    }
                }
            };
            httpClient.DefaultRequestHeaders.Add( "CONVAI-API-KEY", apiKey );
            return httpClient;
        }
        private static HttpContent CreateHttpContent ( Dictionary<string, object> dataToSend ) {
            // Serialize the dictionary to JSON
            string json = JsonConvert.SerializeObject( dataToSend );

            // Convert JSON to HttpContent
            return new StringContent( json, Encoding.UTF8, "application/json" );
        }

        private static HttpContent CreateHttpContent ( string json ) {
            // Convert JSON to HttpContent
            return new StringContent( json, Encoding.UTF8, "application/json" );
        }

        private static async Task<string> SendPostRequestAsync ( string endpoint, HttpClient httpClient, HttpContent content ) {
            try {
                HttpResponseMessage response = await httpClient.PostAsync( endpoint, content );
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch ( HttpRequestException e ) {
                ConvaiLogger.Error( $"Request to {endpoint} failed: {e.Message}", ConvaiLogger.LogCategory.GRPC );
                return null;
            }
        }
        #endregion
        #region END POINTS

        private const string NEW_SPEAKER = "user/speaker/new";
        private const string SPEAKER_ID_LIST = "user/speaker/list";
        private const string DELETE_SPEAKER_ID = "user/speaker/delete";
        private const string CHARACTER_UPDATE = "character/update";
        private const string CHARACTER_GET = "character/get";

        #endregion
    }
}