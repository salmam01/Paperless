namespace Paperless.BL.Exceptions
{
    public enum ExceptionType
    {
        Validation,
        Messaging,
        Storage,
        Search,
        Parsing,
        Internal
    }

    public class ServiceException : Exception
    {
        public ExceptionType Type { get; }

        public ServiceException (string message, ExceptionType exceptionType, Exception? innerException = null)
            : base(message, innerException)
        {
            Type = exceptionType;
        }
    }
}
