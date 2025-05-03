using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.Interfaces
{
    public abstract class VoiceChatPlayerInterface : IDisposable
    {
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }
        public int BytesPerSample { get; private set; }

        public float Volume { get; set; }

        public Dictionary<int, VoiceChatAudioSource> VoiceChatAudioSources { get; private set; } = new();

        public VoiceChatPlayerInterface(int sampleRate, int channels, int bytesPerSample)
        {
            SampleRate = sampleRate;
            Channels = channels;
            BytesPerSample = bytesPerSample;
            Volume = 1;
        }

        /// <summary>
        /// Get all playback device names.
        /// </summary>
        /// <returns>List of playback device names.</returns>
        public abstract List<string> GetPlaybackDeviceNames();

        /// <summary>
        /// Set current device for playback.
        /// </summary>
        /// <param name="name">Device name.</param>
        public abstract void SetCurrentPlaybackDevice(string name);

        /// <summary>
        /// Get current playback device name.
        /// </summary>
        /// <returns>The playback device name.</returns>
        public abstract string GetCurrentPlaybackDeviceName();

        /// <summary>
        /// Play the player.
        /// </summary>
        public abstract void Play();

        /// <summary>
        /// Pause the player.
        /// </summary>
        public abstract void Pause();

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public abstract void Dispose();
    }
}
