using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.DAL.Exceptions
{
    public class DatabaseException : Exception
    {
        public DatabaseException(string message, Exception? innerException = null)
            : base(message, innerException)
        { }
    }
}
