using System.Text.Json.Serialization;

public record ObservationState
{
    [JsonPropertyName("room")]
    public string RoomName { get; init; }

    [JsonPropertyName("x")]
    public float PositionX { get; init; }

    [JsonPropertyName("y")]
    public float PositionY { get; init; }

    [JsonPropertyName("vx")]
    public float VelocityX { get; init; }

    [JsonPropertyName("vy")]
    public float VelocityY { get; init; }

    [JsonPropertyName("dash")]
    public int DashCount { get; init; }

    [JsonPropertyName("onGround")]
    public int OnGround { get; init; }
}
