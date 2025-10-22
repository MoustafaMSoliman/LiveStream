using LiveStream.APPLICATION.DTOs;
using LiveStream.DOMAIN.MediaMTX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveStream.APPLICATION.Interfaces
{
    public interface IMediaMtxService
    {
        Task<bool> AddCameraAsync(string cameraName, MediaMtxPath pathConfig);
        Task<bool> RemoveCameraAsync(string cameraName);
        Task<MediaMtxPathListResponse> GetAllCamerasAsync();
        Task<MediaMtxPathItem?> GetCameraAsync(string cameraName);
    }
}
