using OpusSharp.Core;
using System.Collections.Concurrent;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.NetworkStorageData.Shared;
using VoiceChatSharp.Utils;


namespace VoiceChatSharp.VoiceChat
{
    public enum VoiceChatRecorderErrorCode
    {
        Success = 0,
        EncodedQueueIsEmpty,
        CannotGetTheFirstEncodedAudioPacket,
    }

    /// <summary>
    /// This class will record from a mic and encode it with opus.
    /// </summary>
    public class VoiceChatRecorder : VoiceChat
    {
        VoiceChatRecorderInterface recorderInterface;
        OpusEncoder opusEncoder;

        public ConcurrentQueue<EncodedAudioPacket> encodedAudioPacketsQueue = new();

        byte[] encodedSamples;

        bool isDisposed;

        /// <summary>
        /// The constructor for VoiceChatRecorder
        /// </summary>
        /// <param name="recorderInterface">The recorder interface to use</param>
        public VoiceChatRecorder(VoiceChatRecorderInterface recorderInterface) : base(recorderInterface.SampleRate, recorderInterface.Channels, recorderInterface.BytesPerSample)
        {
            this.recorderInterface = recorderInterface;

            recorderInterface.OnSampleRead += OnSampleRead;

            opusEncoder = new OpusEncoder(sample_rate: recorderInterface.SampleRate, channels: recorderInterface.Channels, application: OpusPredefinedValues.OPUS_APPLICATION_VOIP);

            encodedSamples = new byte[1000];
        }

        /// <summary>
        /// Get all recording device names.
        /// </summary>
        /// <returns>List of recording device names</returns>
        public List<string> GetRecordingDeviceNames()
        {
            return recorderInterface.GetRecordingDeviceNames();
        }

        /// <summary>
        /// Set the device for recording
        /// </summary>
        /// <param name="name">Device name</param>
        public void SetCurrentRecordingDevice(string name)
        {
            recorderInterface.SetCurrentRecordingDevice(name);
        }

        /// <summary>
        /// Get current recording device name.
        /// </summary>
        /// <returns>The recording device name.</returns>
        public string GetCurrentRecordingDeviceName()
        {
            return recorderInterface.GetCurrentRecordingDeviceName();
        }

        /// <summary>
        /// Set recording volume.
        /// </summary>
        public void SetVolume(float volume)
        {
            recorderInterface.SetVolume(volume);
        }

        /// <summary>
        /// Get the first encoded samples recorded from the mic.
        /// </summary>
        /// <returns>The encoded samples.</returns>
        public EncodedAudioPacket? GetTheFirstEncodedAudioPacket()
        {
            if (encodedAudioPacketsQueue.Count is 0)
            {
                //Logger.LogError("Encoded queue is empty");
                return null;
            }

            if (!encodedAudioPacketsQueue.TryDequeue(out EncodedAudioPacket encodedAudioPacket))
            {
                //Logger.LogError("Cannot get the first encoded samples");
                return null;
            }

            return encodedAudioPacket;
        }

        /// <summary>
        /// Start mic recording.
        /// </summary>
        public void StartRecording()
        {
            recorderInterface.StartRecording();
        }

        /// <summary>
        /// Stop mic recording.
        /// </summary>
        public void StopRecording()
        {
            recorderInterface.StopRecording();
        }

        /// <summary>
        /// This method get called internally for each samples that is coming from the mic.
        /// </summary>
        /// <param name="samples">The samples span.</param>
        void OnSampleRead(Span<float> samples)
        {
            Span<byte> encodedOutputSpan = new Span<byte>(encodedSamples);

            int frameSize = Helper.GetTotalBytes(SampleRate, recorderInterface.FrameSizeMS, Channels, BytesPerSample) / sizeof(float) / Channels;

            int encodedOutputLength = opusEncoder.Encode(samples, frameSize, encodedSamples, encodedSamples.Length);

            Span<byte> encodedSlice = encodedOutputSpan.Slice(0, encodedOutputLength);

            encodedAudioPacketsQueue.Enqueue(new EncodedAudioPacket(DateTimeOffset.Now.ToUnixTimeMilliseconds(), encodedSlice.ToArray()));
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            recorderInterface.Dispose();
            opusEncoder.Dispose();
        }
    }

}
