using OpusSharp.Core;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.Core
{
    public class VoiceChatPlayer : VoiceChat
    {
        public OpusDecoder OpusDecoder { get; private set; }

        VoiceChatPlayerInterface voiceChatPlayerInterface;

        Dictionary<int, VoiceChatAudioSource> voiceChatAudioSources = new();

        CancellationTokenSource cancellationTokenSource = new();


        public VoiceChatPlayer(VoiceChatPlayerInterface voiceChatPlayerInterface) : base(voiceChatPlayerInterface.SampleRate, voiceChatPlayerInterface.Channels)
        {
            OpusDecoder = new OpusDecoder(sample_rate: voiceChatPlayerInterface.SampleRate, channels: voiceChatPlayerInterface.Channels);

            this.voiceChatPlayerInterface = voiceChatPlayerInterface;
        }

        public void QueueEncodedSample(int id, byte[] encodedSample)
        {
            if (voiceChatAudioSources.ContainsKey(id))
            {
                voiceChatAudioSources[id].EnqueueEncodedSample(encodedSample);
            }
        }

        public void SetVolume(int id, float volume)
        {
            voiceChatAudioSources[id].SetVolume(volume);
        }

        public bool ContainsVoiceChatAudioSource(int id)
        {
            return voiceChatAudioSources.ContainsKey(id);
        }

        /// <summary>
        /// Add audio source to the audio player
        /// </summary>
        /// <typeparam name="T">VoiceChatAudioInterface</typeparam>
        /// <param name="id"></param>
        public void AddVoiceChatAudioSource<T>(int id) where T : VoiceChatAudioSourceInterface, new()
        {
            voiceChatAudioSources[id] = new VoiceChatAudioSource(new T());
            voiceChatAudioSources[id].VoiceChatAudioSourceInterface.Init(SampleRate, Channels, OpusDecoder);
            voiceChatPlayerInterface.BindAudioSource(voiceChatAudioSources[id]);

            voiceChatAudioSources[id].Play();
        }

        public void RemoveVoiceChatAudioSource(int id)
        {
            if (voiceChatAudioSources.ContainsKey(id))
            {
                voiceChatAudioSources[id].Pause();
                voiceChatPlayerInterface.UnbindAudioSource(voiceChatAudioSources[id]);
                voiceChatAudioSources[id].Dispose();
                voiceChatAudioSources.Remove(id);
            }
        }

        /// <summary>
        /// Play the player.
        /// </summary>
        public void Play()
        {
            Task.Factory.StartNew(() =>
            {
                SpinWait spinWait = new();

                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    foreach (var voiceChatAudioSource in voiceChatAudioSources.Values)
                    {
                        voiceChatAudioSource.Update();
                    }

                    spinWait.SpinOnce();
                }

            }, cancellationTokenSource.Token);

            voiceChatPlayerInterface.Play();
        }

        /// <summary>
        /// Pause the player.
        /// </summary>
        public void Pause()
        {
            cancellationTokenSource.Cancel();
            voiceChatPlayerInterface.Pause();
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {
            cancellationTokenSource.Cancel();

            foreach (VoiceChatAudioSource voiceChatAudioSource in voiceChatAudioSources.Values)
            {
                voiceChatAudioSource.Dispose();
            }

            voiceChatAudioSources.Clear();

            voiceChatPlayerInterface.Dispose();
            OpusDecoder.Dispose();
        }
    }
}