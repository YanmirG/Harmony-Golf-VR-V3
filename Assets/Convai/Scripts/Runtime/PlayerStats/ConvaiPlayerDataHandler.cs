using System;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.PlayerStats.API;
using UnityEngine;

namespace Convai.Scripts.Runtime.PlayerStats {
    public class ConvaiPlayerDataHandler : MonoBehaviour {
        public const string PLAYER_NAME_SAVE_KEY = "PlayerName";
        public const string SPEAKER_ID_SAVE_KEY = "PlayerSpeakerID";
        [field: SerializeField] public ConvaiPlayerDataSO ConvaiPlayerDataSO { get; private set; }

        private void Awake () {
            InitializeDataContainer();
        }

        private async void Start () {
            LoadData();

            if ( !ConvaiPlayerDataSO.CreateSpeakerIDIfNotFound )
                return;
            if ( !string.IsNullOrEmpty( ConvaiPlayerDataSO.SpeakerID ) )
                return;
            await SetNewSpeakerID();
        }

        private void OnDestroy () {
            SaveData();
        }

        private async Task SetNewSpeakerID () {
            string speakerID = await CreateSpeakerID( ConvaiPlayerDataSO.PlayerName, true, true,
                () => { ConvaiLogger.DebugLog( "Could not create a new speaker ID, please try again", ConvaiLogger.LogCategory.Editor ); } );
            if ( !string.IsNullOrEmpty( speakerID ) )
                ConvaiPlayerDataSO.SpeakerID = speakerID;
        }

        /// <summary>
        ///     Loads the Player data from Player Prefs
        /// </summary>
        public void LoadData () {
            if ( ConvaiPlayerDataSO == null )
                return;
            ConvaiPlayerDataSO.PlayerName =
                PlayerPrefs.GetString( PLAYER_NAME_SAVE_KEY, ConvaiPlayerDataSO.DefaultPlayerName );
            ConvaiPlayerDataSO.SpeakerID = PlayerPrefs.GetString( SPEAKER_ID_SAVE_KEY, string.Empty );
        }

        /// <summary>
        ///     Saves the Player data to Player Prefs
        /// </summary>
        public void SaveData () {
            if ( ConvaiPlayerDataSO == null )
                return;
            PlayerPrefs.SetString( PLAYER_NAME_SAVE_KEY, ConvaiPlayerDataSO.PlayerName );
            PlayerPrefs.SetString( SPEAKER_ID_SAVE_KEY, ConvaiPlayerDataSO.SpeakerID );
        }

        /// <summary>
        ///     Sends a request to Convai API to create a new speaker ID
        /// </summary>
        /// <param name="playerName">Player name which will be used to create new Speaker ID</param>
        /// <param name="randomPrefix">Decides if this function will add a random prefix to make the player name unique</param>
        /// <param name="randomSuffix">Decides if this function will add a random suffix to make the player name unique</param>
        /// <returns></returns>
        public async Task<string> CreateSpeakerID ( string playerName, bool randomPrefix = false, bool randomSuffix = false, Action onFail = null ) {
            if ( !ConvaiAPIKeySetup.GetAPIKey( out string apiKey ) )
                return string.Empty;
            string response = await LongTermMemoryAPI.CreateNewSpeakerID( apiKey, playerName, onFail );
            ConvaiLogger.DebugLog( $"Created SpeakerID = {response ?? "Unsuccessful"} for Player Name = {playerName}", ConvaiLogger.LogCategory.Character );
            return response;
        }

        #region Class Utility

        private string CreateRandomString ( int length ) {
            return Guid.NewGuid().ToString().Substring( 0, length );
        }

        private void InitializeDataContainer () {
            if ( ConvaiPlayerDataSO != null )
                return;
            if ( !ConvaiPlayerDataSO.GetPlayerData( out ConvaiPlayerDataSO dataSO ) )
                return;
            ConvaiPlayerDataSO = dataSO;
        }

        #endregion
    }
}