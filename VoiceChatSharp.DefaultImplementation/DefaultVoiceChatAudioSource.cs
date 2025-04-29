using OpusSharp.Core;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatAudioSource : VoiceChatAudioSourceInterface
    {
        //public unsafe SDL_AudioStream* AudioStream { get; private set; }

        //unsafe nint decodedSamplePtr;

        public override void Init(int sampleRate, int channels, OpusDecoder opusDecoder)
        {
            base.Init(sampleRate, channels, opusDecoder);

            //unsafe
            //{
            //    decodedSamplePtr = (nint)NativeMemory.Alloc((nuint)VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels));
            //}

            //SDL_AudioSpec audioSpec = new();
            //audioSpec.format = SDL3.SDL_AUDIO_S16;
            //audioSpec.freq = SampleRate;
            //audioSpec.channels = Channels;

            //unsafe
            //{
            //    AudioStream = SDL3.SDL_CreateAudioStream(&audioSpec, &audioSpec);
            //}
        }

        public override void Update()
        {
            //unsafe
            //{
            //    int sampleSize = VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels);

            //    if (SDL3.SDL_GetAudioStreamQueued(AudioStream) > sampleSize)
            //    {
            //        return;
            //    }

            //    if (!EncodedSampleQueue.TryDequeue(out byte[]? sample))
            //    {
            //        return;
            //    }

            //    Span<byte> decodedSample = new Span<byte>((void*)decodedSamplePtr, VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels));

            //    OpusDecoder.Decode(sample, sample.Length, decodedSample, decodedSample.Length, false);

            //    SDL3.SDL_PutAudioStreamData(AudioStream, decodedSamplePtr, VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels));
            //}
        }


        public override void Play()
        {
            //unsafe
            //{
            //    SDL3.SDL_ResumeAudioStreamDevice(AudioStream);
            //}
        }

        public override void SetVolume(float volume)
        {

        }

        public override void Pause()
        {
            //unsafe
            //{
            //    SDL3.SDL_PauseAudioStreamDevice(AudioStream);
            //}
        }

        public override void Dispose()
        {
            //unsafe
            //{
            //    NativeMemory.Free((void*)decodedSamplePtr);
            //}

            //unsafe
            //{
            //    SDL3.SDL_DestroyAudioStream(AudioStream);
            //}
        }
    }
}
