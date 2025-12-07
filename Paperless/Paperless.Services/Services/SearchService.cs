using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Services
{
    public class SearchService
    {
        private readonly ILogger _logger;
        public SearchService(ILogger<SearchService> logger)
        {
            _logger = logger;
        }
    }
}
