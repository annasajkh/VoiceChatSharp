using OpusSharp.Core;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.VoiceChat
{
    public class VoiceChatPlayer : VoiceChat
    {
        public OpusDecoder OpusDecoder { get; private set; }

        VoiceChatPlayerInterface voiceChatPlayerInterface;

        Thread updateThread;

        public VoiceChatPlayer(VoiceChatPlayerInterface voiceChatPlayerInterface) : base(voiceChatPlayerInterface.SampleRate, voiceChatPlayerInterface.Channels, voiceChatPlayerInterface.BytesPerSample)
        {
            OpusDecoder = new OpusDecoder(sample_rate: voiceChatPlayerInterface.SampleRate, channels: voiceChatPlayerInterface.Channels);

            this.voiceChatPlayerInterface = voiceChatPlayerInterface;

            updateThread = new Thread(UpdateThread);
            updateThread.Start();
        }

        public void QueueEncodedSample(int id, byte[] encodedSample)
        {
            if (voiceChatPlayerInterface.VoiceChatAudioSources.ContainsKey(id))
            {
                voiceChatPlayerInterface.VoiceChatAudioSources[id].EnqueueEncodedSample(encodedSample);
            }
        }


        /// <summary>
        /// Set volume for voice chat player.
        /// </summary>
        public void SetVolumeAudioSource(int id, float volume)
        {
            voiceChatPlayerInterface.VoiceChatAudioSources[id].SetVolume(volume);
        }

        /// <summary>
        /// Play specific audio source.
        /// </summary>
        public void PlayAudioSource(int id)
        {
            voiceChatPlayerInterface.VoiceChatAudioSources[id].Play();
        }

        /// <summary>
        /// Pause specific audio source.
        /// </summary>
        public void PauseAudioSource(int id)
        {
            voiceChatPlayerInterface.VoiceChatAudioSources[id].Pause();
        }

        public bool ContainsVoiceChatAudioSource(int id)
        {
            return voiceChatPlayerInterface.VoiceChatAudioSources.ContainsKey(id);
        }

        public VoiceChatAudioSource GetVoiceChatAudioSource(int id)
        {
            return voiceChatPlayerInterface.VoiceChatAudioSources[id];
        }

        /// <summary>
        /// Add audio source to the audio player
        /// </summary>
        /// <typeparam name="T">VoiceChatAudioInterface</typeparam>
        /// <param name="id"></param>
        public void AddVoiceChatAudioSource<T>(int id) where T : VoiceChatAudioSourceInterface, new()
        {
            voiceChatPlayerInterface.VoiceChatAudioSources[id] = new VoiceChatAudioSource(new T());
            voiceChatPlayerInterface.VoiceChatAudioSources[id].VoiceChatAudioSourceInterface.Init(SampleRate, Channels, BytesPerSample, OpusDecoder);

            voiceChatPlayerInterface.AddVoiceChatAudioSourceCallback<T>(id);
        }

        public void RemoveVoiceChatAudioSource(int id)
        {
            if (voiceChatPlayerInterface.VoiceChatAudioSources.ContainsKey(id))
            {
                voiceChatPlayerInterface.RemoveVoiceChatAudioSourceCallback(id);

                voiceChatPlayerInterface.VoiceChatAudioSources[id].Dispose();
                voiceChatPlayerInterface.VoiceChatAudioSources.Remove(id);
            }
        }

        /// <summary>
        /// Get all playback device names.
        /// </summary>
        /// <returns>List of playback device names.</returns>
        public List<string> GetPlaybackDeviceNames()
        {
            return voiceChatPlayerInterface.GetPlaybackDeviceNames();
        }

        /// <summary>
        /// Set current device for playback.
        /// </summary>
        /// <param name="name">Device name.</param>
        public void SetCurrentPlaybackDevice(string name)
        {
            voiceChatPlayerInterface.SetCurrentPlaybackDevice(name);
        }

        /// <summary>
        /// Get current playback device name.
        /// </summary>
        /// <returns>The playback device name.</returns>
        public string GetCurrentPlaybackDeviceName()
        {
            return voiceChatPlayerInterface.GetCurrentPlaybackDeviceName();
        }

        /// <summary>
        /// Set the volume of the voice chat player
        /// </summary>
        /// <param name="volume">The volume</param>
        public void SetVolume(float volume)
        {
            voiceChatPlayerInterface.SetVolume(volume);
        }

        void UpdateThread()
        {
            SpinWait spinWait = new();

            while (true)
            {
                if (voiceChatPlayerInterface.Playing)
                {
                    foreach (var voiceChatAudioSource in voiceChatPlayerInterface.VoiceChatAudioSources.Values)
                    {
                        voiceChatAudioSource.Update();
                    }
                }

                spinWait.SpinOnce();
            }
        }

        /// <summary>
        /// Play the player.
        /// </summary>
        public void Play()
        {
            voiceChatPlayerInterface.Play();
        }

        /// <summary>
        /// Pause the player.
        /// </summary>
        public void Pause()
        {
            voiceChatPlayerInterface.Pause();
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {
            foreach (VoiceChatAudioSource voiceChatAudioSource in voiceChatPlayerInterface.VoiceChatAudioSources.Values)
            {
                voiceChatAudioSource.Dispose();
            }

            voiceChatPlayerInterface.VoiceChatAudioSources.Clear();

            voiceChatPlayerInterface.Dispose();
            OpusDecoder.Dispose();
        }
    }
}
