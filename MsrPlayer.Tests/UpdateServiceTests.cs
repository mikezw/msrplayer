using MsrPlayer.Services;
using Xunit;

namespace MsrPlayer.Tests;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.0.0", "1.1.0")]
    [InlineData("1.0.0", "1.0.1")]
    [InlineData("1.2.3", "1.10.0")]
    [InlineData("0.9.0", "1.0.0")]
    public void IsNewerVersion_LatestIsNewer_ReturnsTrue(string current, string latest)
    {
        Assert.True(UpdateService.IsNewerVersion(current, latest));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("1.1.0", "1.0.0")]
    [InlineData("1.10.0", "1.2.3")]
    public void IsNewerVersion_LatestIsNotNewer_ReturnsFalse(string current, string latest)
    {
        Assert.False(UpdateService.IsNewerVersion(current, latest));
    }

    [Theory]
    [InlineData("1.0.0", "abc")]
    [InlineData("abc", "1.1.0")]
    [InlineData("1.0.0", "1.1.0-beta.1")]
    [InlineData("", "1.1.0")]
    public void IsNewerVersion_InvalidVersion_ReturnsFalse(string current, string latest)
    {
        Assert.False(UpdateService.IsNewerVersion(current, latest));
    }

    [Theory]
    [InlineData(null, "1.1.0")]
    [InlineData("1.0.0", null)]
    [InlineData(null, null)]
    public void IsNewerVersion_NullVersion_ReturnsFalse(string? current, string? latest)
    {
        Assert.False(UpdateService.IsNewerVersion(current, latest));
    }
}
