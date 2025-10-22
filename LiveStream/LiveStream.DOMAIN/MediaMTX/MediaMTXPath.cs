using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveStream.DOMAIN.MediaMTX
{
    public class MediaMtxPath
    {
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool SourceOnDemand { get; set; } = true;
        public string? SourceOnDemandStartTimeout { get; set; } = "30s";
        public string? SourceOnDemandCloseAfter { get; set; } = "0s";
    }
}
