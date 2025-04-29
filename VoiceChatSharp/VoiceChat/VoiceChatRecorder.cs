using OpusSharp.Core;
using RNNoise.NET;
using System.Collections.Concurrent;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;


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
        VoiceChatRecorderInterface recorderInterface;
        OpusEncoder opusEncoder;
        Denoiser denoiser = new();

        float[] floatSample;
        byte[] encodedOutput;

        public ConcurrentQueue<byte[]> encodedSampleQueue = new();

        bool useNoiseSuppression;

        /// <summary>
        /// The constructor for VoiceChatRecorder
        /// </summary>
        /// <param name="recorderInterface">The recorder interface to use</param>
        /// <param name="useNoiseSuppression">whenever using noise suppression or not it uses RNNoise library</param>
        public VoiceChatRecorder(VoiceChatRecorderInterface recorderInterface, bool useNoiseSuppression = true) : base(recorderInterface.SampleRate, recorderInterface.Channels)
        {
            this.recorderInterface = recorderInterface;
            recorderInterface.OnSampleRead += OnSampleRead;

            opusEncoder = new OpusEncoder(sample_rate: recorderInterface.SampleRate, channels: recorderInterface.Channels, application: OpusPredefinedValues.OPUS_APPLICATION_VOIP);

            this.useNoiseSuppression = useNoiseSuppression;

            floatSample = new float[VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels) / sizeof(float)];
            encodedOutput = new byte[1000];
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
        /// <param name="sample">The sample span.</param>
        void OnSampleRead(Span<byte> sample)
        {
            Span<float> floatSampleSpan = new Span<float>(floatSample);

            VoiceUtils.Convert16BitToFloat(sample, floatSampleSpan);

            if (useNoiseSuppression)
            {
                denoiser.Denoise(floatSampleSpan);
            }

            VoiceUtils.ConvertFloatTo16Bit(floatSampleSpan, sample);

            Span<byte> encodedOutputSpan = new Span<byte>(encodedOutput);

            int encodedOutputLength;

            encodedOutputLength = opusEncoder.Encode(sample, sample.Length, encodedOutputSpan, encodedOutputSpan.Length);

            Span<byte> encodedSlice = encodedOutputSpan.Slice(0, encodedOutputLength);

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