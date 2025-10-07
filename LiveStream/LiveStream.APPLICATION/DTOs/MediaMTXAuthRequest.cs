using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveStream.APPLICATION.DTOs
{
    public class MediaMTXAuthRequest
    {
        public string Ip { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string Action { get; set; }
        public string Path { get; set; }
        public string Protocol { get; set; }
        public string Id { get; set; }
        public string Query { get; set; }
    }
}
