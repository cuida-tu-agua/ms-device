using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Domain.Tests;

public class DeviceSecretsTests
{
    [Theory]
    [InlineData(" sw-esp32-000001 ", "SW-ESP32-000001")]
    [InlineData("SW-ESP32-000001", "SW-ESP32-000001")]
    public void NormalizeSerial_trims_and_uppercases(string input, string expected) =>
        Assert.Equal(expected, DeviceSecrets.NormalizeSerial(input));

    [Theory]
    [InlineData((string?)null)]
    [InlineData("")]
    [InlineData("SW")]
    [InlineData("SW_ESP32/000001")]
    [InlineData("SW-ESP32-000001-THIS-IS-WAY-TOO-LONG")]
    public void NormalizeSerial_rejects_bad_formats(string? input) =>
        Assert.Throws<InvalidDeviceDataException>(() => DeviceSecrets.NormalizeSerial(input));

    [Theory]
    [InlineData("4hzx-vcdp")]
    [InlineData("4HZX VCDP")]
    [InlineData(" 4HZXVCDP ")]
    public void NormalizePairingCode_accepts_dashes_spaces_and_lowercase(string input) =>
        Assert.Equal("4HZXVCDP", DeviceSecrets.NormalizePairingCode(input));

    [Theory]
    [InlineData("4HZX-VCD")]      // 7 characters
    [InlineData("4HZX-VCDPQ")]    // 9 characters
    [InlineData("0HZX-VCDP")]     // 0 is not in the alphabet (looks like O)
    [InlineData("IHZX-VCDP")]     // I is not in the alphabet (looks like 1)
    public void NormalizePairingCode_rejects_bad_codes(string input) =>
        Assert.Throws<InvalidDeviceDataException>(() => DeviceSecrets.NormalizePairingCode(input));

    [Fact]
    public void Hashes_are_the_same_as_the_sql_seed()
    {
        // Values of SW-ESP32-000001 in 02-dml/00-inserts/001-seed-dev-devices.sql (HASHBYTES SHA2_256)
        Assert.Equal("396c70da64fcf7c8636e21a856da594f8bcf27fb4654bf4c6c9c3b6749d3cdb5",
            DeviceSecrets.HashToken(TestDevices.Token));
        Assert.Equal("ca6e420e5e7e59afeb21695389d189b4cc2abe2aa48392a4f8cd72c670fd52f3",
            DeviceSecrets.HashPairingCode(TestDevices.Serial, TestDevices.Code));
    }
}
