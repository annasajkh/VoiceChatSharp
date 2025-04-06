namespace VoiceChatSharp.Interfaces
{
    public abstract class RecorderInterface : IDisposable
    {
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }

        public delegate void AudioRead(Span<float> samples);

        public event AudioRead? OnAudioRead;

        public RecorderInterface(int sampleRate = 48000, int channels = 2)
        {
            SampleRate = sampleRate;
            Channels = channels;
        }

        /// <summary>
        /// Call this method on the implementation for each audio pcm sample that is coming out
        /// </summary>
        /// <param name="samples">The sample coming out</param>
        protected void OnAudioReadInternal(Span<float> samples)
        {
            OnAudioRead?.Invoke(samples);
        }

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