using OpusSharp.Core;
using System.Collections.Concurrent;
using VoiceChatSharp.Interfaces;


namespace VoiceChatSharp.Core
{
    public enum VoiceChatRecorderErrorCode
    {
        Success = 0,
        EncodedQueueIsEmpty,
        CannotGetTheFirstEncodedSample,
    }

    /// <summary>
    /// This class will record from a mic and encode it with opus.
    /// </summary>
    public class VoiceChatRecorder : VoiceChat
    {
        RecorderInterface recorderInterface;
        OpusEncoder opusEncoder;

        ConcurrentQueue<byte[]> encodedSampleQueue = new ConcurrentQueue<byte[]>();

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="sampleRate">The sample rate, this must be one of 8000, 12000, 16000, 24000, or 48000.</param>
        /// <param name="channels">The number of channels. Defaults to 2 (stereo).</param>
        public VoiceChatRecorder(RecorderInterface recorderInterface) : base(recorderInterface.SampleRate, recorderInterface.Channels)
        {
            this.recorderInterface = recorderInterface;
            recorderInterface.OnAudioRead += OnAudioRead;

            opusEncoder = new OpusEncoder(sample_rate: recorderInterface.SampleRate, channels: recorderInterface.Channels, application: OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        }

        /// <summary>
        /// Get the first encoded sample recorded from the mic.
        /// </summary>
        /// <returns>The encoded sample.</returns>
        public byte[]? GetTheFirstEncodedSample()
        {
            if (encodedSampleQueue.Count is 0)
            {
                //Logger.LogError("Encoded queue is empty");
                return null;
            }

            if (!encodedSampleQueue.TryDequeue(out byte[]? result))
            {
                //Logger.LogError("Cannot get the first encoded sample");
                return null;
            }

            return result;
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
        /// This method get called internally for each sample that is coming from the mic.
        /// </summary>
        /// <param name="samples">The sample span.</param>
        void OnAudioRead(Span<float> samples)
        {
            Span<byte> encodedOutput = stackalloc byte[1000];

            int encodedOutputLength = opusEncoder.Encode(samples, SamplesPerFrame / Channels, encodedOutput, encodedOutput.Length);

            Span<byte> encodedSlice = encodedOutput.Slice(0, encodedOutputLength);

            encodedSampleQueue.Enqueue(encodedSlice.ToArray());
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {
            recorderInterface.Dispose();
            opusEncoder.Dispose();
        }
    }

}