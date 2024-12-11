using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Convai.Scripts.Runtime.Addons
{
    /// <summary>
    ///     Class for handling player movement including walking, running, jumping, and looking around.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Player Movement")]
    [HelpURL("https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview")]
    public class ConvaiPlayerMovement : MonoBehaviour
    {
        [Header("Movement Parameters")] [SerializeField] [Tooltip("The speed at which the player walks.")] [Range(1, 10)]
        private float walkingSpeed = 3f;

        [SerializeField] [Tooltip("The speed at which the player runs.")] [Range(1, 10)]
        private float runningSpeed = 8f;

        [SerializeField] [Tooltip("The speed at which the player jumps.")] [Range(1, 10)]
        private float jumpSpeed = 4f;

        [Header("Gravity & Grounding")] [SerializeField] [Tooltip("The gravity applied to the player.")] [Range(1, 10)]
        private float gravity = 9.8f;

        [Header("Camera Parameters")] [SerializeField] [Tooltip("The main camera the player uses.")]
        private Camera playerCamera;

        [SerializeField] [Tooltip("Speed at which the player can look around.")] [Range(0, 1)]
        private float lookSpeedMultiplier = 0.5f;

        [SerializeField] [Tooltip("Limit of upwards and downwards look angles.")] [Range(1, 90)]
        private float lookXLimit = 45.0f;

        private CharacterController _characterController;
        private Vector3 _moveDirection = Vector3.zero;
        private float _rotationX;

        //Singleton Instance
        public static ConvaiPlayerMovement Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern to ensure only one instance exists
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Check for running state and move the player
            MovePlayer();

            // Handle the player and camera rotation
            RotatePlayerAndCamera();
        }

        private void OnEnable()
        {
            ConvaiInputManager.Instance.jumping += Jump;
        }


        private void MovePlayer()
        {
            Vector3 horizontalMovement = Vector3.zero;

            if (!EventSystem.current.IsPointerOverGameObject() && !UIUtilities.IsAnyInputFieldFocused())
            {
                Vector3 forward = transform.TransformDirection(Vector3.forward);
                Vector3 right = transform.TransformDirection(Vector3.right);

                float speed = ConvaiInputManager.Instance.isRunning ? runningSpeed : walkingSpeed;

                Vector2 moveVector = ConvaiInputManager.Instance.moveVector;
                float curSpeedX = speed * moveVector.x;
                float curSpeedY = speed * moveVector.y;

                horizontalMovement = forward * curSpeedY + right * curSpeedX;
            }

            if (!_characterController.isGrounded)
                // Apply gravity only when canMove is true
                _moveDirection.y -= gravity * Time.deltaTime;

            // Move the character
            _characterController.Move((_moveDirection + horizontalMovement) * Time.deltaTime);
        }

        private void Jump()
        {
            if (_characterController.isGrounded && !UIUtilities.IsAnyInputFieldFocused()) _moveDirection.y = jumpSpeed;
        }

        private void RotatePlayerAndCamera()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            // Vertical rotation
            _rotationX -= ConvaiInputManager.Instance.lookVector.y * lookSpeedMultiplier;
            _rotationX = Mathf.Clamp(_rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(_rotationX, 0, 0);

            // Horizontal rotation
            float rotationY = ConvaiInputManager.Instance.lookVector.x * lookSpeedMultiplier;
            transform.rotation *= Quaternion.Euler(0, rotationY, 0);
        }
    }
}