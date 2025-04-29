using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.Core
{
    public class VoiceChatAudioSource : IDisposable
    {        
        public VoiceChatAudioSourceInterface VoiceChatAudioSourceInterface { get; private set; }

        public VoiceChatAudioSource(VoiceChatAudioSourceInterface voiceChatAudioSourceInterface)
        {
            VoiceChatAudioSourceInterface = voiceChatAudioSourceInterface;
        }

        public void EnqueueEncodedSample(byte[] encodedSample)
        {
            VoiceChatAudioSourceInterface.EncodedSampleQueue.Enqueue(encodedSample);
        }

        /// <summary>
        /// Set the volume of the audio source
        /// </summary>
        /// <param name="volume">The volume</param>
        public void SetVolume(float volume)
        {
            VoiceChatAudioSourceInterface.SetVolume(volume);
        }

        /// <summary>
        /// Play the audio source
        /// </summary>
        public void Play()
        {
            VoiceChatAudioSourceInterface.Play();
        }

        public void Update()
        {
            VoiceChatAudioSourceInterface.Update();
        }

        /// <summary>
        /// Pause the audio source
        /// </summary>
        public void Pause()
        {
            VoiceChatAudioSourceInterface.Pause();
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public void Dispose()
        {
            VoiceChatAudioSourceInterface.Dispose();
        }
    }
}
