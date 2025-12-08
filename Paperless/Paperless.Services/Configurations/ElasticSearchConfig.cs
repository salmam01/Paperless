using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Configurations
{
    public class ElasticSearchConfig
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Index { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
