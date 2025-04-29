using VoiceChatSharp.Core;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatPlayer : VoiceChatPlayerInterface
    {
        //SDL_AudioDeviceID deviceID;

        public DefaultVoiceChatPlayer(int sampleRate = 48000, int channels = 2) : base(sampleRate, channels)
        {
            //if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO))
            //{
            //    throw new Exception($"Cannot init sdl audio: {SDL3.SDL_GetError()}");
            //}

            //SDL_AudioSpec audioSpec = new();
            //audioSpec.format = SDL3.SDL_AUDIO_S16;
            //audioSpec.freq = SampleRate;
            //audioSpec.channels = Channels;

            //unsafe
            //{
            //    deviceID = SDL3.SDL_OpenAudioDevice(SDL3.SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &audioSpec);
            //}
        }

        public override void BindAudioSource(VoiceChatAudioSource voiceChatAudioSource)
        {
            //if (voiceChatAudioSource.VoiceChatAudioSourceInterface is DefaultVoiceChatAudioSource defaultVoiceChatAudioSource)
            //{
            //    unsafe
            //    {
            //        if (!SDL3.SDL_BindAudioStream(deviceID, defaultVoiceChatAudioSource.AudioStream))
            //        {
            //            throw new Exception("Cannot bind to audio stream");
            //        }
            //    }
            //}
            //else
            //{
            //    throw new Exception("DefaultVoiceChatPlayer should bind to DefaultVoiceChatAudioSource");
            //}
        }

        public override void UnbindAudioSource(VoiceChatAudioSource voiceChatAudioSource)
        {
            //if (voiceChatAudioSource.VoiceChatAudioSourceInterface is DefaultVoiceChatAudioSource defaultVoiceChatAudioSource)
            //{
            //    unsafe
            //    {
            //        SDL3.SDL_UnbindAudioStream(defaultVoiceChatAudioSource.AudioStream);
            //    }
            //}
            //else
            //{
            //    throw new Exception("DefaultVoiceChatPlayer should bind to DefaultVoiceChatAudioSource");
            //}
        }

        /// <summary>
        /// Play the audio source.
        /// </summary>
        public override void Play()
        {
            //unsafe
            //{
            //    SDL3.SDL_ResumeAudioDevice(deviceID);
            //}
        }

        /// <summary>
        /// Pause the audio source.
        /// </summary>
        public override void Pause()
        {
            //unsafe
            //{
            //    SDL3.SDL_PauseAudioDevice(deviceID);
            //}

        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {

        }
    }
}
