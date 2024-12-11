using System;
using System.Collections;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     This class is used to control the microphone test.
    ///     It requires a UIMicrophoneSettings, AudioSource, and MicrophoneInputChecker components to work.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(UIMicrophoneSettings), typeof(AudioSource), typeof(MicrophoneInputChecker))]
    public class MicrophoneTestController : MonoBehaviour
    {
        /// <summary>
        ///     Constants for frequency and maximum recording time.
        /// </summary>
        private const int FREQUENCY = 44100;

        private const int MAX_RECORD_TIME = 10;

        /// <summary>
        ///     The AudioSource component attached to this GameObject.
        /// </summary>
        private AudioSource _audioSource;

        /// <summary>
        ///     The Audio Playing Status.
        /// </summary>
        private bool _isAudioPlaying;

        /// <summary>
        ///     The Recording Status.
        /// </summary>
        private bool _isRecording;

        /// <summary>
        ///     The MicrophoneInputChecker component attached to this GameObject.
        /// </summary>
        private MicrophoneInputChecker _microphoneInputChecker;

        /// <summary>
        ///     Coroutine reference for stopping the recording.
        /// </summary>
        private Coroutine _recordTimeCounterCoroutine;

        /// <summary>
        ///     The currently selected microphone device.
        /// </summary>
        private string _selectedDevice;

        /// <summary>
        ///     The UIMicrophoneSettings component attached to this GameObject.
        /// </summary>
        private UIMicrophoneSettings _uiMicrophoneSettings;

        /// <summary>
        ///     Singleton instance of the MicrophoneTestController.
        /// </summary>
        public static MicrophoneTestController Instance { get; private set; }

        /// <summary>
        ///     Get the components on Awake.
        /// </summary>
        private void Awake()
        {
            // Ensure there's only one instance of MicrophoneTestController
            if (Instance != null)
            {
                ConvaiLogger.DebugLog("<color=red> There's More Than One MicrophoneTestController </color> " + transform + " - " + Instance, ConvaiLogger.LogCategory.UI);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Get the components
            _uiMicrophoneSettings = GetComponent<UIMicrophoneSettings>();
            _audioSource = GetComponent<AudioSource>();
            _microphoneInputChecker = GetComponent<MicrophoneInputChecker>();
        }

        /// <summary>
        ///     Add click event listeners for record and stop buttons on Start.
        /// </summary>
        private void Start()
        {
            _uiMicrophoneSettings.GetRecordControllerButton().onClick.AddListener(RecordController);
            _selectedDevice = MicrophoneManager.Instance.SelectedMicrophoneName;
        }

        /// <summary>
        ///     Events for different states of recording.
        /// </summary>
        public event Action OnRecordStarted;

        public event Action OnRecordCompleted;
        public event Action OnAudioClipCompleted;

        /// <summary>
        ///     Check if the selected microphone device is working.
        /// </summary>
        public void CheckMicrophoneDeviceWorkingStatus(AudioClip audioClip)
        {
            _microphoneInputChecker.IsMicrophoneWorking(audioClip);
        }

        /// <summary>
        ///     Handle the Record/Stop button click.
        /// </summary>
        private void RecordController()
        {
            if (_isRecording)
                StopMicrophoneTestRecording();
            else if (!_isAudioPlaying) StartMicrophoneTestRecording();
        }

        /// <summary>
        ///     Start recording from the selected microphone device.
        /// </summary>
        private void StartMicrophoneTestRecording()
        {
            if (_uiMicrophoneSettings.GetMicrophoneSelectDropdown().options.Count > 0)
            {
                _selectedDevice = MicrophoneManager.Instance.SelectedMicrophoneName;
                AudioClip recordedClip = Microphone.Start(_selectedDevice, false, MAX_RECORD_TIME, FREQUENCY);
                _audioSource.clip = recordedClip;
                CheckMicrophoneDeviceWorkingStatus(recordedClip);

                OnRecordStarted?.Invoke();
                _isRecording = true;

                _recordTimeCounterCoroutine = StartCoroutine(RecordTimeCounter());
            }
            else
            {
                ConvaiLogger.Error("No microphone devices found.", ConvaiLogger.LogCategory.UI);
            }
        }

        /// <summary>
        ///     Stop recording from the selected microphone device.
        /// </summary>
        private void StopMicrophoneTestRecording()
        {
            if (Microphone.IsRecording(_selectedDevice))
            {
                StopCoroutine(_recordTimeCounterCoroutine);
                int position = Microphone.GetPosition(_selectedDevice);

                Microphone.End(_selectedDevice);

                TrimAudio(position);
                _audioSource.Play();
                _isAudioPlaying = true;

                OnRecordCompleted?.Invoke();
                _isRecording = false;

                StartCoroutine(AudioClipTimeCounter(_audioSource.clip.length));
            }
        }

        /// <summary>
        ///     Coroutine to automatically stop recording after MAX_RECORD_TIME.
        /// </summary>
        private IEnumerator RecordTimeCounter()
        {
            yield return new WaitForSeconds(MAX_RECORD_TIME);
            StopMicrophoneTestRecording();
        }

        /// <summary>
        ///     Coroutine to invoke OnAudioClipCompleted after the audio clip duration.
        /// </summary>
        private IEnumerator AudioClipTimeCounter(float length)
        {
            yield return new WaitForSeconds(length);

            OnAudioClipCompleted?.Invoke();
            _isAudioPlaying = false;
        }

        /// <summary>
        ///     Trim the audio based on the last recorded position.
        /// </summary>
        private void TrimAudio(int micRecordLastPosition)
        {
            if (_audioSource.clip == null)
            {
                ConvaiLogger.Error("AudioSource clip is null.", ConvaiLogger.LogCategory.UI);
                return;
            }

            if (micRecordLastPosition <= 0)
            {
                ConvaiLogger.Error("Microphone position is zero or negative. Cannot trim audio.", ConvaiLogger.LogCategory.UI);
                return;
            }

            AudioClip tempAudioClip = _audioSource.clip;
            int channels = tempAudioClip.channels;
            int position = micRecordLastPosition;
            float[] samplesArray = new float[position * channels];
            tempAudioClip.GetData(samplesArray, 0);
            AudioClip newClip = AudioClip.Create("RecordedSound", position * channels, channels, FREQUENCY, false);
            newClip.SetData(samplesArray, 0);
            _audioSource.clip = newClip;
        }
    }
}