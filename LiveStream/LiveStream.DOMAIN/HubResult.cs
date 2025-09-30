namespace LiveStream.DOMAIN;

public class HubResult
{
    public bool Success { get; set; }
    public string Error { get; set; }

    public static HubResult Successful() => new HubResult { Success = true };
    public static HubResult Failure(string error) => new HubResult { Success = false, Error = error };
}
public class HubResult<T> : HubResult
{
    public T Data { get; set; }

    public static HubResult<T> Successful(T data) => new HubResult<T> { Success = true, Data = data };
    public static new HubResult<T> Failure(string error) => new HubResult<T> { Success = false, Error = error };
}
