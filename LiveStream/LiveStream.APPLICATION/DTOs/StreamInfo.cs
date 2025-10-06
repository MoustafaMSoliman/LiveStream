using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveStream.APPLICATION.DTOs
{
    public class StreamInfo
    {
        public string WebRTCUrl { get; set; }
        public string HLSUrl { get; set; }
        public string RTSPUrl { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
