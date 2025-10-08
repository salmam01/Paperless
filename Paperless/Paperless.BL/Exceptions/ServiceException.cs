using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Exceptions
{
    public enum ExceptionType
    {
        Validation,
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
