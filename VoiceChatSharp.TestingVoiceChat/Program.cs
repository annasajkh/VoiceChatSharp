using VoiceChatSharp.Core;
using VoiceChatSharp.DefaultImplementation;

namespace VoiceChatSharp.TestingVoiceChat;

internal class Program
{
    static void Main(string[] args)
    {
        VoiceChatRecorder voiceChatRecorder = new VoiceChatRecorder(new DefaultRecorder());
        VoiceChatPlayer voiceChatPlayer = new VoiceChatPlayer(new DefaultAudioSource());

        voiceChatRecorder.StartRecording();
        voiceChatPlayer.AddVoiceChatAudioSource(0);

        while (true)
        {
            byte[]? encodedSample = voiceChatRecorder.GetTheFirstEncodedSample();

            if (encodedSample is null)
            {
                continue;
            }

            voiceChatPlayer.QueueEncodedSample(0, encodedSample);
        }
    }
}
