using System.IO;
using Convai.Scripts.Runtime.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Convai.Scripts.Runtime.PlayerStats {
    [CreateAssetMenu( menuName = "Convai/Player Data", fileName = nameof( ConvaiPlayerDataSO ) )]
    public class ConvaiPlayerDataSO : ScriptableObject {
        [field: SerializeField] public string DefaultPlayerName { get; private set; } = "Player";
        [field: SerializeField] public string PlayerName { get; set; }

        [field: ReadOnly]
        [field: SerializeField]
        public string SpeakerID { get; set; }

        [field: SerializeField] public bool CreateSpeakerIDIfNotFound { get; private set; } = false;

        /// <summary>
        ///     Returns the PlayerDataSO if found in the Resources folder
        /// </summary>
        /// <param name="playerDataSO">Reference of the Player Data</param>
        /// <returns>Returns true if found otherwise false</returns>
        public static bool GetPlayerData ( out ConvaiPlayerDataSO playerDataSO ) {
            playerDataSO = Resources.Load<ConvaiPlayerDataSO>( nameof( ConvaiPlayerDataSO ) );
#if UNITY_EDITOR
            if ( playerDataSO == null ) {
                playerDataSO = CreateInstance<ConvaiPlayerDataSO>();
                CreatePlayerDataSO( playerDataSO );
            }
#endif
            return playerDataSO != null;
        }

#if UNITY_EDITOR
        public static void CreatePlayerDataSO ( ConvaiPlayerDataSO convaiPlayerData ) {
            string assetPath = "Assets/Convai/Resources/ConvaiPlayerDataSO.asset";

            if ( !File.Exists( assetPath ) ) {
                if ( !AssetDatabase.IsValidFolder( "Assets/Convai/Resources" ) )
                    AssetDatabase.CreateFolder( "Assets/Convai", "Resources" );

                AssetDatabase.CreateAsset( convaiPlayerData, assetPath );
            }
            else {
                AssetDatabase.DeleteAsset( assetPath );
                AssetDatabase.CreateAsset( convaiPlayerData, assetPath );
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}