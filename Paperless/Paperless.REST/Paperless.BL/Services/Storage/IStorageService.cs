using Paperless.BL.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services.Storage
{
    public interface IStorageService
    {
        Task StoreDocumentAsync(Document document, Stream content);
        Task DeleteDocumentAsync(Guid id, string type);
        Task DeleteDocumentsAsync();

    }
}
