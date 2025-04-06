namespace VoiceChatSharp.Interfaces
{
    public abstract class AudioSourceInterface : IDisposable
    {
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }

        public AudioSourceInterface(int sampleRate = 48000, int channels = 2)
        {
            SampleRate = sampleRate;
            Channels = channels;
        }

        public delegate void AudioRead(Span<float> samples);

        public event AudioRead? OnAudioRead;

        /// <summary>
        /// Call this method on the implementation for each audio pcm sample that is coming in
        /// </summary>
        /// <param name="samples">The sample coming in</param>
        protected void OnAudioReadInternal(Span<float> samples)
        {
            OnAudioRead?.Invoke(samples);
        }

        /// <summary>
        /// Play the audio source.
        /// </summary>
        public abstract void Play();

        /// <summary>
        /// Stop the audio source.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public abstract void Dispose();
    }
}
