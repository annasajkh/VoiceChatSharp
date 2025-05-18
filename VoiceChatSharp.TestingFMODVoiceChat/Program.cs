using VoiceChatSharp.FMODImplementation;
using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.TestingFMODVoiceChat;

internal class Program
{
    static void Main(string[] args)
    {
        using VoiceChatRecorder voiceChatRecorder = new VoiceChatRecorder(new FMODVoiceChatRecorder());

    }
}
