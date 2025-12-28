using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services.Messaging
{
    public interface IDocumentPublisher
    {
        Task PublishDocumentAsync(Guid id);
    }
}
