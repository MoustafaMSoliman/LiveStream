using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveStream.DOMAIN;

public class ViewerSession
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public bool IsActive { get; set; }
}
