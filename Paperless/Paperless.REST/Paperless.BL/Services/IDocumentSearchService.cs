using Paperless.BL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services
{
    public interface IDocumentSearchService
    {
        //  Get document
        Task<List<SearchResult>> SearchAsync(string query);
    }
}
