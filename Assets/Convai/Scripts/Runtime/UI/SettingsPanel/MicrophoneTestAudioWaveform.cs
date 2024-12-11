using System.Collections;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;

namespace Convai.Scripts.Runtime.UI
{
    [RequireComponent(typeof(MicrophoneTestController))]
    public class MicrophoneTestAudioWaveform : MonoBehaviour
    {
        [Tooltip("The UI element to display the audio waveform.")] [SerializeField]
        private RectTransform waveVisualizerUI;

        /// <summary>
        ///     Array to hold audio sample data.
        /// </summary>
        private readonly float[] _clipSampleData = new float[1024];

        /// <summary>
        ///     Multiplier for adjusting the amplitude of the waveform.
        /// </summary>
        private readonly float _waveMultiplier = 75f;

        /// <summary>
        ///     AudioSource to fetch and play audio from.
        /// </summary>
        private AudioSource _audioSource;

        /// <summary>
        ///     Reference to the MicrophoneManager component.
        /// </summary>
        private MicrophoneTestController _microphoneTestController;

        /// <summary>
        ///     The initial size of the wave visualizer UI.
        /// </summary>
        private Vector2 _startSizeDelta;

        /// <summary>
        ///     Coroutine reference for stopping the waveform display.
        /// </summary>
        private Coroutine _waveformCoroutine;

        /// <summary>
        ///     Get the components on Awake.
        /// </summary>
        private void Awake()
        {
            _microphoneTestController = GetComponent<MicrophoneTestController>();
            _audioSource = GetComponent<AudioSource>();
            _startSizeDelta = waveVisualizerUI.sizeDelta;
        }

        /// <summary>
        ///     Subscribing to events when enabled.
        /// </summary>
        private void OnEnable()
        {
            _microphoneTestController.OnRecordStarted += MicrophoneTestControllerOnRecordStarted;
            _microphoneTestController.OnRecordCompleted += MicrophoneTestControllerOnRecordCompleted;
            _microphoneTestController.OnAudioClipCompleted += MicrophoneTestControllerOnAudioClipCompleted;
        }

        /// <summary>
        ///     Unsubscribing from events when disabled.
        /// </summary>
        private void OnDisable()
        {
            _microphoneTestController.OnRecordStarted -= MicrophoneTestControllerOnRecordStarted;
            _microphoneTestController.OnRecordCompleted -= MicrophoneTestControllerOnRecordCompleted;
            _microphoneTestController.OnAudioClipCompleted -= MicrophoneTestControllerOnAudioClipCompleted;
        }

        /// <summary>
        ///     Called when recording starts.
        /// </summary>
        private void MicrophoneTestControllerOnRecordStarted()
        {
            ConvaiLogger.DebugLog("<color=green> Record Started! </color>", ConvaiLogger.LogCategory.UI);
        }

        /// <summary>
        ///     Called when recording is completed.
        /// </summary>
        private void MicrophoneTestControllerOnRecordCompleted()
        {
            ConvaiLogger.DebugLog("<color=green> Record Completed! </color>", ConvaiLogger.LogCategory.UI);
            _waveformCoroutine = StartCoroutine(DisplayWaveformUntilAudioComplete());
        }

        /// <summary>
        ///     Called when the audio clip playback is completed.
        /// </summary>
        private void MicrophoneTestControllerOnAudioClipCompleted()
        {
            _audioSource.clip = null;
            StopCoroutine(_waveformCoroutine);
            waveVisualizerUI.sizeDelta = _startSizeDelta;
        }

        /// <summary>
        ///     Fetch and display audio waves from the audio source.
        /// </summary>
        private void ShowAudioSourceAudioWaves()
        {
            _audioSource.GetSpectrumData(_clipSampleData, 0, FFTWindow.Rectangular);
            float currentAverageVolume = SampleAverageCalculator() * _waveMultiplier;
            Vector2 size = waveVisualizerUI.sizeDelta;
            size.x = currentAverageVolume;
            size.x = Mathf.Clamp(size.x, 1, 145);
            waveVisualizerUI.sizeDelta = size;
        }

        /// <summary>
        ///     Coroutine to display the waveform while the audio is playing.
        /// </summary>
        private IEnumerator DisplayWaveformUntilAudioComplete()
        {
            while (_audioSource.isPlaying)
            {
                ShowAudioSourceAudioWaves();
                yield return null;
            }
        }

        /// <summary>
        ///     Calculate the average volume from the sampled audio data.
        /// </summary>
        private float SampleAverageCalculator()
        {
            float sum = 0f;
            foreach (float t in _clipSampleData)
                sum += t;

            return sum;
        }
    }
}