namespace Demo07.Interop.RawJson.Messages
{
    // Masstransit을 사용하지 않는 외부 시스템이 전송하는 메시지의 모델
    // "IOT Device의 현 위치를 갱신해줘"
    public interface UpdateLocation
    {
        string DeviceId { get;  }
        int X { get; }
        int Y { get; }
    }
}