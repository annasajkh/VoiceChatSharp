using VoiceChatSharp.Core;
using VoiceChatSharp.DefaultImplementation;
using VoiceChatSharp.Networking;

namespace VoiceChatSharp.TestingClient;

internal class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter your name: ");
        string? name = Console.ReadLine();

        if (name is null)
        {
            throw new Exception("Cannot read console line");
        }

        Console.Write($"Enter the server address: ");

        string? address = Console.ReadLine();

        if (address is null)
        {
            Console.WriteLine("Using default server address of localhost with port 6969");
            address = "localhost:6969";
        }

        if (address.Trim() == "")
        {
            Console.WriteLine("Using default server address of localhost with port 6969");
            address = "localhost:6969";
        }

        string[] addressArr = address.Split(":");

        Console.WriteLine($"Connecting to {addressArr[0]}:{addressArr[1]} with name {name}");

        using var voiceChatClient = new VoiceChatClient<DefaultVoiceChatAudioSource>(new VoiceChatRecorder(new DefaultVoiceChatRecorder()), new VoiceChatPlayer(new DefaultVoiceChatPlayer()), name);

        voiceChatClient.Join(addressArr[0], int.Parse(addressArr[1]), "cat");

        Console.WriteLine("Press any key to leave...");
        while (!Console.KeyAvailable)
        {
            voiceChatClient.Update();
        }

        voiceChatClient.Leave();
        voiceChatClient.Stop();
    }
}
