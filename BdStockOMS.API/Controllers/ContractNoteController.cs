using System;
using System.Threading.Tasks;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractNoteController : ControllerBase
    {
        private readonly IContractNoteService _service;

        public ContractNoteController(IContractNoteService service)
        {
            _service = service;
        }

        [HttpPost("generate/{orderId}")]
        [Authorize(Roles = "SuperAdmin,Admin,Trader")]
        public async Task<IActionResult> Generate(int orderId)
        {
            var result = await _service.GenerateContractNoteAsync(orderId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{contractNoteId}")]
        public async Task<IActionResult> Get(int contractNoteId)
        {
            var result = await _service.GetContractNoteAsync(contractNoteId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetByClient(
            int clientId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _service.GetContractNotesByClientAsync(clientId, from, to);
            return Ok(result);
        }

        [HttpGet("date/{date}")]
        [Authorize(Roles = "SuperAdmin,Admin,Trader")]
        public async Task<IActionResult> GetByDate(DateTime date)
        {
            var result = await _service.GetContractNotesByDateAsync(date);
            return Ok(result);
        }

        [HttpPost("regenerate/{orderId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Regenerate(int orderId)
        {
            var result = await _service.RegenerateContractNoteAsync(orderId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{contractNoteId}/export")]
        public async Task<IActionResult> Export(int contractNoteId)
        {
            var bytes = await _service.ExportContractNotePdfAsync(contractNoteId);
            if (bytes.Length == 0) return NotFound("Contract note not found.");
            return File(bytes, "text/plain", $"ContractNote_{contractNoteId}.txt");
        }
    }
}
