using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services.Storage
{
    public interface IStorageService
    {
        Task StoreDocumentAsync(Guid id, string type, Stream content);
        Task DeleteDocumentAsync(Guid id, string type);
        Task DeleteDocumentsAsync();

    }
}
