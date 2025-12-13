namespace Paperless.BL.Exceptions
{
    public class RabbitMQException : ServiceException
    {
        public RabbitMQException(string message, Exception? innerException = null)
            : base(message, ExceptionType.Messaging, innerException)
        {
        }
    }
}
