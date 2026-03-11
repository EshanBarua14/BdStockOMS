namespace BdStockOMS.API.DTOs.Auth;

public class RegisterInvestorDto
{
    public string FullName         { get; set; } = string.Empty;
    public string Email            { get; set; } = string.Empty;
    public string Password         { get; set; } = string.Empty;
    public string Phone            { get; set; } = string.Empty;
    public int    BrokerageHouseId { get; set; }
    public string? BONumber        { get; set; }
}
