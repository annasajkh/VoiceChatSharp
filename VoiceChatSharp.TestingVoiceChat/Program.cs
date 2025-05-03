using VoiceChatSharp.DefaultImplementation;
using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.TestingVoiceChat;

internal class Program
{
    static void Main(string[] args)
    {
        Random random = new();

        using VoiceChatRecorder voiceChatRecorder = new VoiceChatRecorder(new DefaultVoiceChatRecorder(), useNoiseSuppression: false);
        using VoiceChatPlayer voiceChatPlayer = new VoiceChatPlayer(new DefaultVoiceChatPlayer());

        voiceChatRecorder.StartRecording();

        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(0);
        voiceChatPlayer.Play();

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
