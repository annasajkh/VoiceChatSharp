namespace VoiceChatSharp.Exceptions;

public class VoiceChatPlayerCannotDequeueException : Exception
{
    public VoiceChatPlayerCannotDequeueException() : base()
    {

    }

    public VoiceChatPlayerCannotDequeueException(string? message) : base(message)
    {

    }

    public VoiceChatPlayerCannotDequeueException(string? message, Exception? innerException) : base(message, innerException)
    {

    }
}
