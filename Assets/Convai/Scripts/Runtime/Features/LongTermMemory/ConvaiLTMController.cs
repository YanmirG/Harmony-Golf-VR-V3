using System.Collections;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.PlayerStats.API;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features.LongTermMemory
{
    [AddComponentMenu("Convai/Convai Long Term Memory")]
    public class ConvaiLTMController : MonoBehaviour
    {
        [field: HideInInspector]
        [field: SerializeField]
        public LTMStatus LTMStatus { get; private set; } = LTMStatus.NotDefined;

        private ConvaiNPC _convaiNpc;

        private async void Reset()
        {
            await GetLTMStatus();
        }

        public async Task GetLTMStatus()
        {
            LTMStatus = LTMStatus.NotDefined;
            _convaiNpc = GetComponent<ConvaiNPC>();
            if (!ConvaiAPIKeySetup.GetAPIKey(out string apiKey)) return;
            LTMStatus = await LongTermMemoryAPI.GetLTMStatus(apiKey, _convaiNpc.characterID, OnRequestFailed) ? LTMStatus.Enabled : LTMStatus.Disabled;
        }

        private void OnRequestFailed()
        {
            LTMStatus = LTMStatus.Failed;
        }

        /// <summary>
        ///     It starts a coroutine which can toggle the global status of the LTM for the character.
        ///     It should not be done at runtime since it will toggle the status of the LTM for all the users
        /// </summary>
        /// <param name="enable"> new status of LTM</param>
        /// <returns></returns>
        public IEnumerator ToggleLTM(bool enable)
        {
            if (!ConvaiAPIKeySetup.GetAPIKey(out string apiKey)) yield break;
            LTMStatus = LTMStatus.NotDefined;
            _convaiNpc = GetComponent<ConvaiNPC>();
            Task<bool> resultTask = LongTermMemoryAPI.ToggleLTM(apiKey, _convaiNpc.characterID, enable, OnRequestFailed);
            yield return new WaitUntil(() => resultTask.IsCompleted);
            if (!resultTask.Result) yield break;
            LTMStatus = enable ? LTMStatus.Enabled : LTMStatus.Disabled;
        }
    }
}