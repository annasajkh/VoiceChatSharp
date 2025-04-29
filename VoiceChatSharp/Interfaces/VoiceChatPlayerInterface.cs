using VoiceChatSharp.Core;

namespace VoiceChatSharp.Interfaces
{
    public abstract class VoiceChatPlayerInterface : IDisposable
    {
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }


        public VoiceChatPlayerInterface(int sampleRate, int channels)
        {
            SampleRate = sampleRate;
            Channels = channels;
        }

        public abstract void BindAudioSource(VoiceChatAudioSource voiceChatAudioSource);
        public abstract void UnbindAudioSource(VoiceChatAudioSource voiceChatAudioSource);

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
