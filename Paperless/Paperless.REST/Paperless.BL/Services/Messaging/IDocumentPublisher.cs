using Paperless.BL.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services.Messaging
{
    public interface IDocumentPublisher
    {
        Task PublishDocumentAsync(Guid id, List<Category> categories);
        Task DeleteDocumentAsync(Guid id);
        Task DeleteDocumentsAsync();
    }
}
