namespace VoiceChatSharp.Exceptions
{
    public class SizeMismatchException : Exception
    {
        public SizeMismatchException() : base()
        {

        }

        public SizeMismatchException(string? message) : base(message)
        {

        }

        public SizeMismatchException(string? message, Exception? innerException) : base(message, innerException)
        {

        }
    }

}