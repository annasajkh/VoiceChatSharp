namespace VoiceChatSharp.Interfaces
{
    public abstract class VoiceChatRecorderInterface : IDisposable
    {
        public int SampleRate { get; protected set; }
        public int Channels { get; protected set; }
        public int BytesPerSample { get; protected set; }
        
        public string? RecodingDevice { get; private set; }

        public delegate void SampleFlow(Span<float> samples);

        public event SampleFlow? OnSampleRead;

        public VoiceChatRecorderInterface(int sampleRate, int channels, int bytesPerSample, string? recodingDevice = null)
        {
            SampleRate = sampleRate;
            Channels = channels;
            BytesPerSample = bytesPerSample;
            RecodingDevice = recodingDevice;
        }

        /// <summary>
        /// Call this method on the implementation for each audio pcm samples that is coming out.
        /// </summary>
        /// <param name="samples">The samples coming out</param>
        protected void OnSampleReadInternal(Span<float> samples)
        {
            OnSampleRead?.Invoke(samples);
        }

        /// <summary>
        /// Get all recording device names.
        /// </summary>
        /// <returns>List of recording device names.</returns>
        public abstract List<string> GetRecordingDeviceNames();

        /// <summary>
        /// Set current device for recording.
        /// </summary>
        /// <param name="name">Device name.</param>
        public abstract void SetCurrentRecordingDevice(string name);

        /// <summary>
        /// Get current recording device name.
        /// </summary>
        /// <returns>The recording device name.</returns>
        public abstract string GetCurrentRecordingDeviceName();

        /// <summary>
        /// Set recording volume.
        /// </summary>
        public abstract void SetVolume(float volume);

        /// <summary>
        /// Start recording.
        /// </summary>
        public abstract void StartRecording();

        /// <summary>
        /// Stop recording.
        /// </summary>
        public abstract void StopRecording();

        /// <summary>
        /// Dispose unmanaged resources.
        /// </summary>
        public abstract void Dispose();
    }
}