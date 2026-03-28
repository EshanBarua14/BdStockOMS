using Xunit;
using BdStockOMS.API.Services;
using BdStockOMS.API.Models.Admin;

namespace BdStockOMS.Tests.Unit;

public class Day75Tests
{
    [Fact]
    public void FIXConfigDto_DefaultValues_AreValid()
    {
        var cfg = new FIXConfigDto(
            Enabled: false, SenderCompId: "BDSTOCKOMS", TargetCompId: "DSE",
            Host: "fix.dse.com.bd", Port: 9876, HeartbeatIntervalSec: 30,
            ReconnectIntervalSec: 10, LogMessages: true, UseSSL: true,
            FixVersion: "FIX.4.4", ResetOnLogon: true, ResetOnLogout: true,
            MaxReconnectAttempts: 5, MessageQueueSize: 1000, SendingTimeToleranceSec: 120);
        Assert.Equal("BDSTOCKOMS", cfg.SenderCompId);
        Assert.Equal("DSE",        cfg.TargetCompId);
        Assert.Equal(9876,         cfg.Port);
        Assert.Equal("FIX.4.4",   cfg.FixVersion);
        Assert.False(cfg.Enabled);
        Assert.True(cfg.UseSSL);
        Assert.True(cfg.LogMessages);
    }

    [Fact]
    public void FIXConfigDto_Password_IsNullByDefault()
    {
        var cfg = new FIXConfigDto(
            false,"A","B","host",1234,30,10,true,false,"FIX.4.4",true,true,5,1000,120);
        Assert.Null(cfg.Password);
    }

    [Fact]
    public void FixMessageLog_DirectionValues_AreCorrect()
    {
        var inMsg    = new FixMessageLog { Direction = "IN",    Body = "Logon" };
        var outMsg   = new FixMessageLog { Direction = "OUT",   Body = "Heartbeat" };
        var adminMsg = new FixMessageLog { Direction = "ADMIN", Body = "Config updated" };
        Assert.Equal("IN",    inMsg.Direction);
        Assert.Equal("OUT",   outMsg.Direction);
        Assert.Equal("ADMIN", adminMsg.Direction);
    }

    [Fact]
    public void FixMessageLog_Timestamp_IsAssignable()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var msg    = new FixMessageLog { Timestamp = DateTime.UtcNow };
        Assert.True(msg.Timestamp >= before);
    }

    [Fact]
    public void FIXConfigDto_HeartbeatInterval_IsPositive()
    {
        var cfg = new FIXConfigDto(true,"A","B","host",9876,30,10,true,true,"FIX.4.4",true,true,5,1000,120);
        Assert.True(cfg.HeartbeatIntervalSec > 0);
    }

    [Fact]
    public void FIXConfigDto_Port_InValidRange()
    {
        var cfg = new FIXConfigDto(true,"A","B","host",9876,30,10,true,true,"FIX.4.4",true,true,5,1000,120);
        Assert.InRange(cfg.Port, 1, 65535);
    }

    [Fact]
    public void FIXConfigDto_MaxReconnectAttempts_IsPositive()
    {
        var cfg = new FIXConfigDto(true,"A","B","host",9876,30,10,true,true,"FIX.4.4",true,true,5,1000,120);
        Assert.True(cfg.MaxReconnectAttempts > 0);
    }

    [Fact]
    public void FixMessageLog_SeqNum_IsAssignable()
    {
        var msg = new FixMessageLog { SeqNum = 42 };
        Assert.Equal(42, msg.SeqNum);
    }

    [Fact]
    public void FIXConfigDto_FixVersion_IsKnownValue()
    {
        var known = new[] { "FIX.4.2", "FIX.4.4", "FIX.5.0", "FIXT.1.1" };
        var cfg   = new FIXConfigDto(true,"A","B","h",1234,30,10,true,true,"FIX.4.4",true,true,5,1000,120);
        Assert.Contains(cfg.FixVersion, known);
    }

    [Fact]
    public void FixMessageLog_Body_IsAssignable()
    {
        var msg = new FixMessageLog { Body = "8=FIX.4.4|35=D|49=SENDER" };
        Assert.Contains("FIX.4.4", msg.Body);
    }
}
