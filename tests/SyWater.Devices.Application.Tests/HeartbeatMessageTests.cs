using SyWater.Devices.Application.Heartbeats;

namespace SyWater.Devices.Application.Tests;

public class HeartbeatMessageTests
{
    [Fact]
    public void TryParse_reads_serial_token_and_firmware()
    {
        Assert.True(HeartbeatMessage.TryParse(
            "sywater/devices/sw-esp32-000001/heartbeat", """{"token":"abc","fw":"1.2.0"}""", out var message));

        Assert.Equal(new HeartbeatMessage("SW-ESP32-000001", "abc", "1.2.0"), message);
    }

    [Fact]
    public void TryParse_accepts_a_payload_without_firmware()
    {
        Assert.True(HeartbeatMessage.TryParse("sywater/devices/SW-1/heartbeat", """{"token":"abc"}""", out var message));
        Assert.Null(message!.FirmwareVersion);
    }

    [Theory]
    [InlineData("sywater/devices/SW-1/readings", """{"token":"abc"}""")]      // other topic
    [InlineData("sywater/devices//heartbeat", """{"token":"abc"}""")]        // empty serial
    [InlineData("sywater/devices/a/b/heartbeat", """{"token":"abc"}""")]     // two levels
    [InlineData("sywater/devices/SW-1/heartbeat", "not json")]
    [InlineData("sywater/devices/SW-1/heartbeat", "[]")]
    [InlineData("sywater/devices/SW-1/heartbeat", """{"fw":"1.0"}""")]       // no token
    [InlineData("sywater/devices/SW-1/heartbeat", """{"token":123}""")]      // token is not a string
    [InlineData("sywater/devices/SW-1/heartbeat", """{"token":"  "}""")]     // blank token
    [InlineData(null, "{}")]
    public void TryParse_rejects_malformed_messages(string? topic, string payload) =>
        Assert.False(HeartbeatMessage.TryParse(topic, payload, out _));

    [Fact]
    public void TryParse_rejects_huge_payloads() =>
        Assert.False(HeartbeatMessage.TryParse(
            "sywater/devices/SW-1/heartbeat", "{\"token\":\"" + new string('a', 600) + "\"}", out _));
}
