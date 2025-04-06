using OpusSharp.Core;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.Core
{
    public class VoiceChatPlayer : VoiceChat
    {
        public OpusDecoder OpusDecoder { get; private set; }

        AudioSourceInterface audioSourceInterface;

        Dictionary<int, VoiceChatAudioSource> voiceChatAudioSources = new Dictionary<int, VoiceChatAudioSource>();

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="sampleRate">The sample rate, this must be one of 8000, 12000, 16000, 24000, or 48000.</param>
        /// <param name="channels">The number of channels. Defaults to 2 (stereo).</param>
        public VoiceChatPlayer(AudioSourceInterface audioSourceInterface) : base(audioSourceInterface.SampleRate, audioSourceInterface.Channels)
        {
            OpusDecoder = new OpusDecoder(sample_rate: audioSourceInterface.SampleRate, channels: audioSourceInterface.Channels);
            this.audioSourceInterface = audioSourceInterface;
        }

        public void QueueEncodedSample(int id, byte[] encodedSample)
        {
            voiceChatAudioSources[id].QueueEncodedSample(encodedSample);
        }

        public bool ContainVoiceChatAudioSource(int id)
        {
            return voiceChatAudioSources.ContainsKey(id);
        }

        public void AddVoiceChatAudioSource(int id)
        {
            voiceChatAudioSources[id] = new VoiceChatAudioSource(this, audioSourceInterface);
            voiceChatAudioSources[id].Play();
        }

        public void RemoveVoiceChatAudioSource(int id)
        {
            voiceChatAudioSources[id].Stop();
            voiceChatAudioSources[id].Dispose();
            voiceChatAudioSources.Remove(id);
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {
            OpusDecoder.Dispose();
        }
    }
}