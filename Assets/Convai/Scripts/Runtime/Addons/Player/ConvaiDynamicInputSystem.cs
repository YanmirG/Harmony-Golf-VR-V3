using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;

#elif ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.EventSystems;
#endif

namespace Convai.Scripts.Runtime.Addons
{
    public class ConvaiDynamicInputSystem : MonoBehaviour
    {
        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            if (FindObjectOfType<InputSystemUIInputModule>() == null) gameObject.AddComponent<InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (FindObjectOfType<StandaloneInputModule>() == null)
            {
                gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }
}