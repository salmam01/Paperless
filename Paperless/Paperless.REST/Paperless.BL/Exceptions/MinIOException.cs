namespace Paperless.BL.Exceptions
{
    public class MinIOException : ServiceException
    {
        public MinIOException(string message, Exception? innerException = null)
            : base(message, ExceptionType.Storage, innerException)
        {
        }
    }
}
