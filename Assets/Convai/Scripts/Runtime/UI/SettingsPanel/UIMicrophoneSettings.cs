using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.LoggerSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     This class is used to manage the microphone settings in the UI.
    /// </summary>
    public class UIMicrophoneSettings : MonoBehaviour
    {
        [Tooltip("Dropdown to select the microphone device to use.")] [SerializeField]
        private TMP_Dropdown _microphoneSelectDropdown;

        [Tooltip("Button to control the recording.")] [SerializeField]
        private Button _recordControllerButton;

        [Tooltip("Text to display the status of the recording system.")] [SerializeField]
        private TextMeshProUGUI _recordSystemStatusText;

        /// <summary>
        ///     Image component of the recording control button.
        /// </summary>
        private Image _buttonImage;

        /// <summary>
        ///     Text component of the recording control button.
        /// </summary>
        private TextMeshProUGUI _buttonText;

        /// <summary>
        ///     Reference to the MicrophoneManager to subscribe to its events.
        /// </summary>
        private MicrophoneTestController _microphoneTestController;

        /// <summary>
        ///     Index of the selected microphone device.
        /// </summary>
        private int _selectedMicrophoneDeviceNumber;

        /// <summary>
        ///     Initialize references.
        /// </summary>
        private void Awake()
        {
            _microphoneTestController = GetComponent<MicrophoneTestController>();
            _buttonImage = _recordControllerButton.GetComponent<Image>();
            _buttonText = _recordControllerButton.GetComponentInChildren<TextMeshProUGUI>();

            RequestMicrophonePermissions();
        }

        /// <summary>
        ///     Subscribe to events when this component is enabled.
        /// </summary>
        private void OnEnable()
        {
            _microphoneTestController.OnRecordStarted += MicrophoneTestControllerOnRecordStarted;
            _microphoneTestController.OnRecordCompleted += MicrophoneTestControllerOnRecordCompleted;
            _microphoneTestController.OnAudioClipCompleted += MicrophoneTestControllerOnAudioClipCompleted;

            UISaveLoadSystem.Instance.OnLoad += UISaveLoadSystem_OnLoad;
            UISaveLoadSystem.Instance.OnSave += UISaveLoadSystem_OnSave;

#if !UNITY_ANDROID && !UNITY_IOS
            InitializeMicrophoneDevices();
#endif
        }

        /// <summary>
        ///     Unsubscribe from events when this component is disabled.
        /// </summary>
        private void OnDisable()
        {
            _microphoneTestController.OnRecordStarted -= MicrophoneTestControllerOnRecordStarted;
            _microphoneTestController.OnRecordCompleted -= MicrophoneTestControllerOnRecordCompleted;
            _microphoneTestController.OnAudioClipCompleted -= MicrophoneTestControllerOnAudioClipCompleted;
            UISaveLoadSystem.Instance.OnLoad -= UISaveLoadSystem_OnLoad;
            UISaveLoadSystem.Instance.OnSave -= UISaveLoadSystem_OnSave;
        }

        /// <summary>
        ///     Request Microphone permissions on Android or iOS.
        /// </summary>
        private void RequestMicrophonePermissions()
        {
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                // Initialize Microphone devices if permission is already granted.
                InitializeMicrophoneDevices();
            }
            else
            {
                // Request Microphone permission with callback
                PermissionCallbacks callbacks = new();
                callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
                callbacks.PermissionDenied += s => ShowNoMicrophoneDetectedNotification();
                callbacks.PermissionDeniedAndDontAskAgain += s => ShowNoMicrophoneDetectedNotification();
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
            }

#elif UNITY_IOS
            // Check Microphone permission on iOS and start coroutine to request permission if not granted.
            StartCoroutine(iOSTryToAccessMicrophone());
#endif
        }

        /// <summary>
        ///     Show notification when no microphone is detected.
        /// </summary>
        private void ShowNoMicrophoneDetectedNotification()
        {
            NotificationSystemHandler.Instance.NotificationRequest(NotificationType.NoMicrophoneDetected);
            _recordSystemStatusText.text = "No Microphone Detected...";
        }

        /// <summary>
        ///     Initialize the dropdown with available microphone devices after obtaining permission.
        /// </summary>
        private void InitializeMicrophoneDevices()
        {
            _microphoneSelectDropdown.ClearOptions();
            _microphoneSelectDropdown.AddOptions(new List<string>(Microphone.devices));
            _microphoneSelectDropdown.onValueChanged.AddListener(ChangeSelectedDevice);
            _recordSystemStatusText.text = "Waiting For Record...";

            // Checking if system has at-least one microphone to record the audio
            if (!MicrophoneManager.Instance.HasAnyMicrophoneDevices()) ShowNoMicrophoneDetectedNotification();
        }

        /// <summary>
        ///     Coroutine to check and request Microphone permission on iOS.
        /// </summary>
        private IEnumerator iOSTryToAccessMicrophone()
        {
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                InitializeMicrophoneDevices();
            }
            else
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

                if (Application.HasUserAuthorization(UserAuthorization.Microphone))
                    InitializeMicrophoneDevices();
                else
                    ShowNoMicrophoneDetectedNotification();
            }
        }

        /// <summary>
        ///     Callback when Microphone permission is granted on Android.
        /// </summary>
        /// <param name="obj">Permission string</param>
        private void PermissionCallbacks_PermissionGranted(string obj)
        {
            InitializeMicrophoneDevices();
            ConvaiLogger.Info("Microphone Permission Granted.", ConvaiLogger.LogCategory.Character);
        }

        /// <summary>
        ///     Event handler when the selected microphone device is changed.
        /// </summary>
        private void ChangeSelectedDevice(int selectedDeviceNumber)
        {
            _selectedMicrophoneDeviceNumber = selectedDeviceNumber;
            MicrophoneManager.Instance.SetSelectedMicrophoneIndex(selectedDeviceNumber);
            UISaveLoadSystem.Instance.SelectedMicrophoneDeviceNumber = _selectedMicrophoneDeviceNumber;
            ConvaiLogger.Info("Microphone Device Updated.", ConvaiLogger.LogCategory.Character);
        }

        /// <summary>
        ///     Event handler when saved data is loaded.
        /// </summary>
        private void UISaveLoadSystem_OnLoad()
        {
            _microphoneSelectDropdown.value = UISaveLoadSystem.Instance.SelectedMicrophoneDeviceNumber;
            ConvaiLogger.Info("Loaded Microphone Device. ", ConvaiLogger.LogCategory.Character);
        }

        /// <summary>
        ///     Event handler when data is saved.
        /// </summary>
        private void UISaveLoadSystem_OnSave()
        {
            UISaveLoadSystem.Instance.SelectedMicrophoneDeviceNumber = _microphoneSelectDropdown.value;
        }

        /// <summary>
        ///     Event handler when a recording is started.
        /// </summary>
        private void MicrophoneTestControllerOnRecordStarted()
        {
            _recordSystemStatusText.text = "Recording...";
            _buttonImage.color = Color.red;
            _buttonText.text = "Stop";
            _buttonText.color = new Color(1, 1, 1);
        }

        /// <summary>
        ///     Event handler when a recording is completed.
        /// </summary>
        private void MicrophoneTestControllerOnRecordCompleted()
        {
            _recordSystemStatusText.text = "Playing...";
            _buttonImage.color = Color.green;
            _buttonText.text = "Rec";
            _buttonText.color = new Color(0.14f, 0.14f, 0.14f);
        }

        /// <summary>
        ///     Event handler when the audio clip playback is completed.
        /// </summary>
        private void MicrophoneTestControllerOnAudioClipCompleted()
        {
            _recordSystemStatusText.text = "Waiting For Record...";
        }

        /// <summary>
        ///     Returns the microphone selection dropdown object.
        /// </summary>
        public TMP_Dropdown GetMicrophoneSelectDropdown()
        {
            return _microphoneSelectDropdown;
        }

        /// <summary>
        ///     Returns the record control button object.
        /// </summary>
        public Button GetRecordControllerButton()
        {
            return _recordControllerButton;
        }

        /// <summary>
        ///     Returns the selected microphone device name.
        /// </summary>
        public string GetSelectedMicrophoneDeviceName()
        {
            if (_selectedMicrophoneDeviceNumber < 0 || _selectedMicrophoneDeviceNumber >= _microphoneSelectDropdown.options.Count)
                return string.Empty;

            return _microphoneSelectDropdown.options[_selectedMicrophoneDeviceNumber].text;
        }
    }
}