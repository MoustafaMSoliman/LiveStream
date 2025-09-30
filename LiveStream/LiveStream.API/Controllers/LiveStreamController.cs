using LiveStream.APPLICATION;
using LiveStream.APPLICATION.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LiveStream.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LiveStreamController : ControllerBase
    {
        private readonly IJanusService _janus;

        public LiveStreamController(IJanusService janus)
        {
            _janus = janus;
        }

        [HttpGet("{cameraId}/info")]
        public ActionResult<CameraInfoDto> GetCameraInfo(int cameraId)
        {
            
            return new CameraInfoDto
            ("ws://localhost:8188/janus", // Janus WebSocket server
               20 // الـ stream mountpoint اللي عايزين نتفرج عليه
            );
        }
        [HttpPost("create-rtsp")]
        public async Task<IActionResult> CreateRtsp([FromBody] CreateCameraDto dto)
        {
            var id = await _janus.CreateRtspMountpointAsync(dto);
            if (id == null)
                return BadRequest("Failed to create RTSP mountpoint");

            return Ok(new { cameraId = id });
        }

        [HttpPost("create-rtp")]
        public async Task<IActionResult> CreateRtp([FromQuery] int id, [FromQuery] string name, [FromQuery] int port)
        {
            var session = await _janus.CreateSessionAsync();
            if (session is null) return BadRequest("Failed to create session");

            var handle = await _janus.AttachToStreamingAsync(session.Value);
            var ok = await _janus.CreateCameraAsync(session.Value, handle, id, name, port);

            await _janus.DestroySessionAsync(session.Value);

            if (!ok) return BadRequest("Failed to create RTP camera");
            return Ok(new { cameraId = id, sessionId= session });
        }
    }
}