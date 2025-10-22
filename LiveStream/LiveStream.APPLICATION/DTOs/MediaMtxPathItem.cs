using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LiveStream.APPLICATION.DTOs
{
    public class MediaMtxPathItem
    {
        public string Name { get; set; } = string.Empty;
        [JsonConverter(typeof(SourceConverter))] 
        public string Source { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int Readers { get; set; }
        public DateTime Created { get; set; }
        public DateTime? ReadyTime { get; set; }
        public string? ReadyDuration { get; set; }
        public List<string>? Tracks { get; set; }
        public string? BytesReceived { get; set; }
    }
}
