using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BdStockOMS.API.Services.Interfaces
{
    public interface IContractNoteService
    {
        Task<ContractNoteResult> GenerateContractNoteAsync(int orderId);
        Task<ContractNoteResult> GetContractNoteAsync(int contractNoteId);
        Task<List<ContractNoteSummary>> GetContractNotesByClientAsync(int clientId, DateTime? from, DateTime? to);
        Task<List<ContractNoteSummary>> GetContractNotesByDateAsync(DateTime date);
        Task<ContractNoteResult> RegenerateContractNoteAsync(int orderId);
        Task<byte[]> ExportContractNotePdfAsync(int contractNoteId);
    }

    public class ContractNoteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ContractNoteDto? ContractNote { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ContractNoteDto
    {
        public int Id { get; set; }
        public string ContractNoteNumber { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderReference { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string BoAccountNumber { get; set; } = string.Empty;
        public string TraderName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string InstrumentCode { get; set; } = string.Empty;
        public string InstrumentName { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal ExecutedPrice { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal CdscFee { get; set; }
        public decimal LevyCharge { get; set; }
        public decimal VatOnCommission { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime TradeDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ContractNoteSummary
    {
        public int Id { get; set; }
        public string ContractNoteNumber { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string InstrumentCode { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime TradeDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
