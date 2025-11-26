using Microsoft.AspNetCore.Http;
using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Utilities;

public class ActivityLogHelperTests
{
    [Fact]
    public void GetIpAddress_Prefers_XForwardedFor_FirstIp()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10, 203.0.113.11";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.5");

        var ip = ActivityLogHelper.GetIpAddress(context);

        Assert.Equal("203.0.113.10", ip);
    }

    [Fact]
    public void GetIpAddress_FallsBack_ToRemoteIp()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.5");

        var ip = ActivityLogHelper.GetIpAddress(context);

        Assert.Equal("198.51.100.5", ip);
    }

    [Fact]
    public void GetUserAgent_Truncates_WhenTooLong()
    {
        var longUserAgent = new string('A', 300);
        var context = new DefaultHttpContext();
        context.Request.Headers["User-Agent"] = longUserAgent;

        var ua = ActivityLogHelper.GetUserAgent(context);

        Assert.NotNull(ua);
        Assert.Equal(255, ua!.Length);
    }

    [Fact]
    public void SerializeToJson_ReturnsCamelCase()
    {
        var obj = new { FirstName = "Ada", LastName = "Lovelace" };

        var json = ActivityLogHelper.SerializeToJson(obj);

        Assert.Equal("{\"firstName\":\"Ada\",\"lastName\":\"Lovelace\"}", json);
    }
}

