namespace VoiceChatSharp.Utils;

public enum NetworkLoggerType
{
    Client,
    Server
}


public class NetworkLogger
{
    public NetworkLoggerType NetworkLoggerType { get; private set; }

    public NetworkLogger(NetworkLoggerType networkLoggerType)
    {
        NetworkLoggerType = networkLoggerType;
    }

    public void LogInfo(string message)
    {
        Console.WriteLine($"[{NetworkLoggerType}] {message}");
    }

    public void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{NetworkLoggerType}] {message}");
        Console.ForegroundColor = ConsoleColor.White;
    }

    public void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{NetworkLoggerType}] {message}");
        Console.ForegroundColor = ConsoleColor.White;
    }
}
