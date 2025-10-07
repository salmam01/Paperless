using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Exceptions
{
    public class BusinessException : Exception
    {
        public BusinessException (string message, Exception? innerException = null)
            : base(message, innerException)
        { }
    }
}
