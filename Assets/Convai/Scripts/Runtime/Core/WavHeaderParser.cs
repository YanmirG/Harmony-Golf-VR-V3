using System;

namespace Convai.Scripts.Runtime.Core
{
    public class WavHeaderParser
    {
        public WavHeaderParser(byte[] wavBytes)
        {
            // Ensure the byte array is not null and has enough bytes to contain a header
            if (wavBytes == null || wavBytes.Length < 44)
                throw new ArgumentException("Invalid WAV byte array.");

            // Parse the number of channels (2 bytes at offset 22)
            NumChannels = BitConverter.ToInt16(wavBytes, 22);

            // Parse the sample rate (4 bytes at offset 24)
            SampleRate = BitConverter.ToInt32(wavBytes, 24);

            // Parse the bits per sample (2 bytes at offset 34)
            BitsPerSample = BitConverter.ToInt16(wavBytes, 34);

            // Parse the Sub-chunk size (data size) to help calculate the data length
            DataSize = BitConverter.ToInt32(wavBytes, 40);
        }

        private int SampleRate { get; }
        private int NumChannels { get; }
        private int BitsPerSample { get; }
        private int DataSize { get; }

        public float CalculateDurationSeconds()
        {
            // Calculate the total number of samples in the data chunk
            int totalSamples = DataSize / (NumChannels * (BitsPerSample / 8));

            // Calculate the duration in seconds
            return (float)totalSamples / SampleRate;
        }
    }
}