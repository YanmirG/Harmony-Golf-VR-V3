using System;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Convai.Scripts.Runtime.Core
{
    [DefaultExecutionOrder(-105)]
    public class ConvaiInputManager : MonoBehaviour
#if ENABLE_INPUT_SYSTEM
        , Controls.IPlayerActions
#endif
    {
        [HideInInspector] public Vector2 moveVector;
        [HideInInspector] public Vector2 lookVector;
        public bool isRunning { get; private set; }

        public Action jumping;
        public Action sendText;
        public Action toggleChat;
        public Action toggleSettings;

        public bool IsTalkKeyHeld { get; private set; }
        public Action<bool> talkKeyInteract;

#if ENABLE_INPUT_SYSTEM
        private Controls _controls;
#elif ENABLE_LEGACY_INPUT_MANAGER
        [Serializable]
        public class MovementKeys
        {
            public const KeyCode Forward = KeyCode.W;
            public const KeyCode Backward = KeyCode.S;
            public const KeyCode Right = KeyCode.D;
            public const KeyCode Left = KeyCode.A;
        }

        public KeyCode TextSendKey = KeyCode.Return;
        public KeyCode TextSendAltKey = KeyCode.KeypadEnter;
        public KeyCode TalkKey = KeyCode.T;
        public KeyCode OpenSettingPanelKey = KeyCode.F10;
        public KeyCode RunKey = KeyCode.LeftShift;
        public MovementKeys movementKeys;
        
        public bool WasTalkKeyPressed()
        {
            return Input.GetKeyDown(TalkKey);
        }
#endif

        public static ConvaiInputManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                ConvaiLogger.DebugLog("There's more than one ConvaiInputManager! " + transform + " - " + Instance, ConvaiLogger.LogCategory.UI);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LockCursor(true);
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _controls = new Controls();
            _controls.Player.SetCallbacks(this);
            _controls.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            _controls.Disable();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed) jumping?.Invoke();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            moveVector = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            lookVector = context.ReadValue<Vector2>();
        }

        public void OnMousePress(InputAction.CallbackContext context)
        {
        }

        public void OnRun(InputAction.CallbackContext context)
        {
            if (context.performed) isRunning = !isRunning;
        }

        public void OnSendText(InputAction.CallbackContext context)
        {
            if (context.performed) sendText?.Invoke();
        }

        public void OnToggleChat(InputAction.CallbackContext context)
        {
            if (context.performed) toggleChat?.Invoke();
        }

        public void OnToggleSettings(InputAction.CallbackContext context)
        {
            if (context.performed) toggleSettings?.Invoke();
        }

        public void OnTalk(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                talkKeyInteract?.Invoke(true);
                IsTalkKeyHeld = true;
            }

            if (context.canceled)
            {
                talkKeyInteract?.Invoke(false);
                IsTalkKeyHeld = false;
            }
        }

        public void OnCursorUnlock(InputAction.CallbackContext context)
        {
        }
#endif

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (_controls.Player.MousePress.WasPressedThisFrame() && !EventSystem.current.IsPointerOverGameObject()) LockCursor(true);
            if (_controls.Player.CursorUnlock.WasPressedThisFrame()) LockCursor(false);
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetButton("Jump"))
            {
                jumping?.Invoke();
            }

            moveVector = Vector2.zero;
            if (Input.GetKey(MovementKeys.Forward)) moveVector.y += 1f;
            if (Input.GetKey(MovementKeys.Backward)) moveVector.y -= 1f;
            if (Input.GetKey(MovementKeys.Left)) moveVector.x -= 1f;
            if (Input.GetKey(MovementKeys.Right)) moveVector.x += 1f;

            lookVector.x = Input.GetAxis("Mouse X") * 2f;
            lookVector.y = Input.GetAxis("Mouse Y") * 2f;

            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) LockCursor(true);
            if (Input.GetKeyDown(RunKey)) isRunning = !isRunning;
            if (Input.GetKeyDown(TextSendKey) || Input.GetKeyDown(TextSendAltKey)) sendText?.Invoke();
            if (Input.GetKeyDown(OpenSettingPanelKey)) toggleSettings?.Invoke();
            if (Input.GetKeyDown(TalkKey))
            {
                talkKeyInteract?.Invoke(true);
                IsTalkKeyHeld = true;
            }

            if (Input.GetKeyUp(TalkKey))
            {
                talkKeyInteract?.Invoke(false);
                IsTalkKeyHeld = false;
            }
#endif
        }

        private static void LockCursor(bool lockState)
        {
            Cursor.lockState = lockState ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockState;
        }

#if ENABLE_INPUT_SYSTEM
        public InputAction GetTalkKeyAction()
        {
            return _controls.Player.Talk;
        }
#endif
    }
}