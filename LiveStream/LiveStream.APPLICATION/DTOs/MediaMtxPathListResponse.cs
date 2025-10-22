using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveStream.APPLICATION.DTOs
{
    public class MediaMtxPathListResponse
    {
        public int ItemCount { get; set; }
        public int PageCount { get; set; }
        public List<MediaMtxPathItem> Items { get; set; } = new();
    }
}
