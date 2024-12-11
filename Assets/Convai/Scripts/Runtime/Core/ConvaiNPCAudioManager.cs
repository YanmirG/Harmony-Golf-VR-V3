using System;
using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    public class ConvaiNPCAudioManager : MonoBehaviour
    {
        private readonly Queue<ResponseAudio> _responseAudios = new();
        private AudioSource _audioSource;
        private ConvaiNPC _convaiNPC;
        private ConvaiGroupNPCController _npcController;
        private bool _lastTalkingState;
        private Coroutine _playInOrderCoroutine;
        private bool _stopAudioPlayingLoop;
        private bool _waitForCharacterLipSync;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _convaiNPC = GetComponent<ConvaiNPC>();
            TryGetComponent(out _npcController);
            
            _lastTalkingState = false;
        }

        private void OnEnable()
        {
            _playInOrderCoroutine = StartCoroutine(PlayAudioInOrder());
        }

        private void OnDisable()
        {
            ClearResponseAudioQueue();
            StopAllAudioPlayback();
            _audioSource.clip = null;
            if (_playInOrderCoroutine == null) return;
            StopCoroutine(_playInOrderCoroutine);
        }

        public event Action<string> OnAudioTranscriptAvailable;
        public event Action<bool> OnCharacterTalkingChanged;
        public event Action PurgeExcessLipSyncFrames;

        public void StopAllAudioPlayback()
        {
            if (_audioSource != null && _audioSource.isPlaying) _audioSource.Stop();
        }

        public void ClearResponseAudioQueue()
        {
            _responseAudios.Clear();
        }

        private void SetCharacterTalking(bool isTalking)
        {
            if (_lastTalkingState != isTalking)
            {
                OnCharacterTalkingChanged?.Invoke(isTalking);
                _lastTalkingState = isTalking;
            }
        }

        private void PurgeLipSyncFrames()
        {
            PurgeExcessLipSyncFrames?.Invoke();
        }

        public void AddResponseAudio(ResponseAudio responseAudio)
        {
            _responseAudios.Enqueue(responseAudio);
        }

        public int GetAudioResponseCount()
        {
            return _responseAudios.Count;
        }


        public bool SetWaitForCharacterLipSync(bool value)
        {
            _waitForCharacterLipSync = value;
            return value;
        }

        public IEnumerator PlayAudioInOrder()
        {
            while (!_stopAudioPlayingLoop)
                if (_responseAudios.Count > 0)
                {
                    ResponseAudio currentResponseAudio = _responseAudios.Dequeue();

                    if (!currentResponseAudio.IsFinal)
                    {
                        _audioSource.clip = currentResponseAudio.AudioClip;
                        while (_waitForCharacterLipSync)
                            yield return new WaitForSeconds(0.01f);

                        if (_npcController != null)
                        {
                            while (_npcController.IsOtherNPCTalking())
                            {
                                yield return new WaitForSeconds(0.1f);
                            }
                        }
                        
                        _audioSource.Play();
                        //ConvaiLogger.DebugLog($"Playing: {currentResponseAudio.AudioTranscript}", ConvaiLogger.LogCategory.LipSync);
                        SetCharacterTalking(true);
                        OnAudioTranscriptAvailable?.Invoke(currentResponseAudio.AudioTranscript.Trim());
                        yield return new WaitForSeconds(currentResponseAudio.AudioClip.length);
                        _audioSource.Stop();
                        _audioSource.clip = null;
                        PurgeLipSyncFrames();
                        if (_responseAudios.Count == 0 && _convaiNPC.convaiLipSync != null)
                            SetWaitForCharacterLipSync(true);
                    }
                    else
                    {
                        ConvaiLogger.DebugLog($"Final Playing: {currentResponseAudio.AudioTranscript}", ConvaiLogger.LogCategory.LipSync);
                        SetCharacterTalking(false);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                    SetCharacterTalking(false);
                }
        }

        /// <summary>
        ///     Converts a byte array containing audio data into an AudioClip.
        /// </summary>
        /// <param name="byteAudio">Byte array containing the audio data</param>
        /// <param name="stringSampleRate">String containing the sample rate of the audio</param>
        /// <returns>AudioClip containing the decoded audio data</returns>
        public AudioClip ProcessByteAudioDataToAudioClip(byte[] byteAudio, string stringSampleRate)
        {
            try
            {
                if (byteAudio.Length <= 44)
                    throw new ArgumentException("Not enough data in byte audio to trim the header.", nameof(byteAudio));

                // Trim the 44 bytes WAV header from the byte array to get the actual audio data
                byte[] trimmedByteAudio = new byte[byteAudio.Length - 44];
                for (int i = 0, j = 44; i < byteAudio.Length - 44; i++, j++) trimmedByteAudio[i] = byteAudio[j];

                // Convert the trimmed byte audio data to a float array of audio samples
                float[] samples = Convert16BitByteArrayToFloatAudioClipData(trimmedByteAudio);
                if (samples.Length <= 0) throw new Exception("No samples created after conversion from byte array.");

                const int channels = 1; // Mono audio
                int sampleRate = int.Parse(stringSampleRate); // Convert the sample rate string to an integer

                // Create an AudioClip using the converted audio samples and other parameters
                AudioClip clip = AudioClip.Create("Audio Response", samples.Length, channels, sampleRate, false);

                // Set the audio data for the AudioClip
                clip.SetData(samples, 0);

                return clip;
            }
            catch (Exception)
            {
                // Log or handle exceptions appropriately
                return null;
            }
        }

        /// <summary>
        ///     Converts a byte array representing 16-bit audio samples to a float array.
        /// </summary>
        /// <param name="source">Byte array containing 16-bit audio data</param>
        /// <returns>Float array containing audio samples in the range [-1, 1]</returns>
        private static float[] Convert16BitByteArrayToFloatAudioClipData(byte[] source)
        {
            const int x = sizeof(short); // Size of a short in bytes
            int convertedSize = source.Length / x; // Number of short samples
            float[] data = new float[convertedSize]; // Float array to hold the converted data

            int byteIndex = 0; // Index for the byte array
            int dataIndex = 0; // Index for the float array

            // Convert each pair of bytes to a short and then to a float
            while (byteIndex < source.Length)
            {
                byte firstByte = source[byteIndex];
                byte secondByte = source[byteIndex + 1];
                byteIndex += 2;

                // Combine the two bytes to form a short (little endian)
                short s = (short)((secondByte << 8) | firstByte);

                // Convert the short value to a float in the range [-1, 1]
                data[dataIndex] = s / 32768.0F; // Dividing by 32768.0 to normalize the range
                dataIndex++;
            }

            return data;
        }

        public void StopAudioLoop()
        {
            _stopAudioPlayingLoop = true;
        }

        public class ResponseAudio
        {
            public AudioClip AudioClip;
            public string AudioTranscript;
            public bool IsFinal;
        }
    }
}