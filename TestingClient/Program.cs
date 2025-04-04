using VoiceChatSharp.Networking;

namespace TestingClient;

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
        string[]? address = Console.ReadLine()?.Split(":");

        if (address is null)
        {
            throw new Exception("Cannot read console line");
        }

        Console.WriteLine($"Connecting to {address[0]}:{address[1]} with name {name}");

        VoiceChatClient voiceChatClient = new(name);

        voiceChatClient.Join(address[0], int.Parse(address[1]), "catto");

        Console.WriteLine("Press any key to leave...");
        while (!Console.KeyAvailable)
        {
            voiceChatClient.Update();
        }

        voiceChatClient.Leave();
        voiceChatClient.Stop();
    }
}
