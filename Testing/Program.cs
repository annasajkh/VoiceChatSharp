using DotNext;
using VoiceChatSharp.Core;

namespace TestingCore;

internal class Program
{
    static void Main(string[] args)
    {
        VoiceChatRecorder voiceChatRecorder = new();
        VoiceChatPlayer voiceChatPlayer = new();

        voiceChatRecorder.StartRecording();
        voiceChatPlayer.AddVoiceChatAudioSource(0, new VoiceChatAudioSource(voiceChatPlayer));

        while (true)
        {
            Result<byte[], VoiceChatRecorderErrorCode> encodedSampleResult = voiceChatRecorder.GetTheFirstEncodedSample();

            if (!encodedSampleResult.TryGet(out byte[] encodedSample))
            {
                switch (encodedSampleResult.Error)
                {
                    case VoiceChatRecorderErrorCode.EncodedQueueIsEmpty:
                        continue;
                    case VoiceChatRecorderErrorCode.CannotGetTheFirstEncodedSample:
                        throw new Exception("Error: Cannot get the first encoded sample");
                }
            }
            voiceChatPlayer.QueueEncodedSample(0, encodedSample);
        }
    }
}
