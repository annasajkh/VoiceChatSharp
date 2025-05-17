using VoiceChatSharp.Interfaces;
using VoiceChatSharp.NetworkStorageData.Shared;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.VoiceChat
{
    public class VoiceChatAudioSource : IDisposable
    {
        public VoiceChatAudioSourceInterface VoiceChatAudioSourceInterface { get; private set; }

        public long TimeForAudioSamplesToArrive { get; private set; }

        int sampleNeededToAvoidAudioDeviceFromGettingDried;
        bool isSampling;

        bool isDisposed;

        public VoiceChatAudioSource(VoiceChatAudioSourceInterface voiceChatAudioSourceInterface)
        {
            VoiceChatAudioSourceInterface = voiceChatAudioSourceInterface;
        }

        public void EnqueueEncodedAudioPacket(EncodedAudioPacket encodedAudioPacket)
        {
            VoiceChatAudioSourceInterface.EncodedAudioPacketsQueue.Enqueue(encodedAudioPacket);
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
            if (!VoiceChatAudioSourceInterface.IsAudioDeviceWantSamples())
            {
                return;
            }

            if (!isSampling)
            {
                if (VoiceChatAudioSourceInterface.EncodedAudioPacketsQueue.Count < sampleNeededToAvoidAudioDeviceFromGettingDried)
                {
                    return;
                }
                else
                {
                    isSampling = true;
                }
            }
            else
            {
                if (VoiceChatAudioSourceInterface.EncodedAudioPacketsQueue.Count < sampleNeededToAvoidAudioDeviceFromGettingDried / 4)
                {
                    isSampling = false;
                }
            }

            if (!VoiceChatAudioSourceInterface.EncodedAudioPacketsQueue.TryDequeue(out EncodedAudioPacket encodedAudioPacket))
            {
                return;
            }

            TimeForAudioSamplesToArrive = DateTimeOffset.Now.ToUnixTimeMilliseconds() - encodedAudioPacket.PacketTimeMS;

            sampleNeededToAvoidAudioDeviceFromGettingDried = (int)(TimeForAudioSamplesToArrive / VoiceChatAudioSourceInterface.FrameSizeMS) * 2;

            Span<float> decodedSamples;

            int totalSamplesBytes = Helper.GetTotalBytes(VoiceChatAudioSourceInterface.SampleRate, VoiceChatAudioSourceInterface.FrameSizeMS, VoiceChatAudioSourceInterface.Channels, VoiceChatAudioSourceInterface.BytesPerSample);

            unsafe
            {
                decodedSamples = new Span<float>((void*)VoiceChatAudioSourceInterface.DecodedSamplesPtr, totalSamplesBytes / sizeof(float));
            }

            VoiceChatAudioSourceInterface.OpusDecoder.Decode(encodedAudioPacket.Data, encodedAudioPacket.Data.Length, decodedSamples, totalSamplesBytes / sizeof(float) / VoiceChatAudioSourceInterface.Channels, false);
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
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            VoiceChatAudioSourceInterface.Dispose();
        }
    }
}
