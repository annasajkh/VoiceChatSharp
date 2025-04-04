using VoiceChatSharp.Networking;

namespace TestingServer;

internal class Program
{
    static void Main(string[] args)
    {
        VoiceChatServer voiceChatServer = new("catto");
        voiceChatServer.Start(6969);

        while (!Console.KeyAvailable)
        {
            voiceChatServer.Update();
        }

        voiceChatServer.Stop();
    }
}
