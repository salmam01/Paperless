namespace Paperless.BL.Exceptions
{
    public class ElasticSearchException : ServiceException
    {
        public ElasticSearchException(string message, Exception? innerException = null)
            : base(message, ExceptionType.Storage, innerException)
        {
        }
    }
}
