using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
