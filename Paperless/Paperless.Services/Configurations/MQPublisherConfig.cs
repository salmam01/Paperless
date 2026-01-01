using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Configurations
{
    public class MQPublisherConfig
    {
        public List<string> RoutingKeys { get; set; } = [];
    }
}
