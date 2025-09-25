namespace LiveStream.APPLICATION;

public record CreateCameraDto(
    int? Id,         
    string Name,     
    string RtspUrl  
);
