using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN.MediaMTX;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LiveStream.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CamerasController : ControllerBase
    {
        private readonly IMediaMtxService _mediaMtxService;
        private readonly ILogger<CamerasController> _logger;

        public CamerasController(IMediaMtxService mediaMtxService, ILogger<CamerasController> logger)
        {
            _mediaMtxService = mediaMtxService;
            _logger = logger;
        }

        [HttpPost("add/{cameraName}")]
        public async Task<IActionResult> AddCamera(string cameraName, [FromBody] AddCameraRequest request)
        {
            if (string.IsNullOrEmpty(cameraName) || string.IsNullOrEmpty(request.Source))
            {
                return BadRequest("Camera name and source are required");
            }

            var pathConfig = new MediaMtxPath
            {
                Source = request.Source,
                SourceOnDemand = request.SourceOnDemand,
                SourceOnDemandStartTimeout = request.SourceOnDemandStartTimeout
            };

            var result = await _mediaMtxService.AddCameraAsync(cameraName, pathConfig);

            if (result)
            {
                return Ok(new { message = $"Camera {cameraName} added successfully" });
            }
            else
            {
                return StatusCode(500, new { error = $"Failed to add camera {cameraName}" });
            }
        }

        [HttpDelete("remove/{cameraName}")]
        public async Task<IActionResult> RemoveCamera(string cameraName)
        {
            if (string.IsNullOrEmpty(cameraName))
            {
                return BadRequest("Camera name is required");
            }

            var result = await _mediaMtxService.RemoveCameraAsync(cameraName);

            if (result)
            {
                return Ok(new { message = $"Camera {cameraName} removed successfully" });
            }
            else
            {
                return StatusCode(500, new { error = $"Failed to remove camera {cameraName}" });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllCameras()
        {
            var cameras = await _mediaMtxService.GetAllCamerasAsync();
            return Ok(cameras);
        }

        [HttpGet("get/{cameraName}")]
        public async Task<IActionResult> GetCamera(string cameraName)
        {
            if (string.IsNullOrEmpty(cameraName))
            {
                return BadRequest("Camera name is required");
            }

            var camera = await _mediaMtxService.GetCameraAsync(cameraName);

            if (camera != null)
            {
                return Ok(camera);
            }
            else
            {
                return NotFound(new { error = $"Camera {cameraName} not found" });
            }
        }
    }

    public class AddCameraRequest
    {
        public string Source { get; set; } = string.Empty;
        public bool SourceOnDemand { get; set; } = true;
        public string? SourceOnDemandStartTimeout { get; set; } = "30s";
    }
}
