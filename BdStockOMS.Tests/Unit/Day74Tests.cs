using Xunit;
using BdStockOMS.API.Services;
using BdStockOMS.API.Models;

namespace BdStockOMS.Tests.Unit;

public class Day74Tests
{
    private static string Md5(string s)
    {
        var b = System.Text.Encoding.UTF8.GetBytes(s);
        return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(b)).ToLowerInvariant();
    }
    private static bool Verify(string content, string expected)
        => string.Equals(Md5(content), expected.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);

    [Fact] public void Md5_SameInput_SameHash() => Assert.Equal(Md5("hello"), Md5("hello"));
    [Fact] public void Md5_DiffInput_DiffHash() => Assert.NotEqual(Md5("abc"), Md5("xyz"));
    [Fact] public void Verify_Correct_True()    { var c = "TestContent"; Assert.True(Verify(c, Md5(c))); }
    [Fact] public void Verify_Wrong_False()     => Assert.False(Verify("real", "aaaabbbbccccdddd1234567890abcdef"));
    [Fact] public void Verify_CaseInsensitive() { var c = "Case"; Assert.True(Verify(c, Md5(c).ToUpperInvariant())); }

    [Fact] public void ParseClients_Valid_TwoRecords()
    {
        var xml = "<Clients><Client><BOAccountNumber>1111</BOAccountNumber><ClientName>Alice</ClientName></Client><Client><BOAccountNumber>2222</BOAccountNumber></Client></Clients>";
        var res = new BosXmlService(null!).ParseClientsXml(xml);
        Assert.Equal(2, res.Count);
        Assert.Equal("1111", res[0].BoAccountNumber);
        Assert.Equal("Alice", res[0].ClientName);
    }

    [Fact] public void ParseClients_Empty_ReturnsEmpty()
        => Assert.Empty(new BosXmlService(null!).ParseClientsXml("<Clients></Clients>"));

    [Fact] public void ParsePositions_Valid_OneRecord()
    {
        var xml = "<Positions><Position><BOAccountNumber>1111</BOAccountNumber><StockCode>BRAC</StockCode><Quantity>500</Quantity><AveragePrice>45.50</AveragePrice></Position></Positions>";
        var res = new BosXmlService(null!).ParsePositionsXml(xml);
        Assert.Single(res);
        Assert.Equal("BRAC", res[0].StockCode);
        Assert.Equal(500m,   res[0].Quantity);
        Assert.Equal(45.50m, res[0].AveragePrice);
    }

    [Fact] public void ParsePositions_BadDecimal_Zero()
    {
        var xml = "<Positions><Position><StockCode>X</StockCode><Quantity>notanumber</Quantity></Position></Positions>";
        Assert.Equal(0m, new BosXmlService(null!).ParsePositionsXml(xml)[0].Quantity);
    }

    [Fact] public void ExtractMd5_Valid_ReturnsMd5()
        => Assert.Equal("abcdef1234567890abcdef1234567890",
               new BosXmlService(null!).ExtractMd5FromCtrl("<Control><MD5>abcdef1234567890abcdef1234567890</MD5></Control>"));

    [Fact] public void ExtractMd5_Missing_Empty()
        => Assert.Equal(string.Empty, new BosXmlService(null!).ExtractMd5FromCtrl("<Control></Control>"));

    [Fact] public void ExtractMd5_Malformed_Empty()
        => Assert.Equal(string.Empty, new BosXmlService(null!).ExtractMd5FromCtrl("not xml <<<"));

    [Fact] public void BosImportSession_DefaultStatus_Pending()
        => Assert.Equal("Pending", new BosImportSession().Status);

    [Fact] public void BosReconciliationResult_UnmatchedItems_Empty()
        => Assert.Empty(new BosReconciliationResult().UnmatchedItems);
}
