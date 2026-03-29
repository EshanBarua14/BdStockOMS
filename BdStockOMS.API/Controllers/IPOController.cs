using System.Threading.Tasks;
using BdStockOMS.API.DTOs.IPO;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/ipo")]
    [Authorize]
    public class IPOController : ControllerBase
    {
        private readonly IIPOService _svc;
        public IPOController(IIPOService svc) => _svc = svc;

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateIPODto dto)
        {
            var r = await _svc.CreateIPOAsync(dto);
            return r.IsSuccess ? CreatedAtAction(nameof(Get), new { id = r.Value!.Id }, r.Value) : BadRequest(r.Error);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var r = await _svc.GetIPOAsync(id);
            return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var r = await _svc.GetAllIPOsAsync(status);
            return Ok(r.Value);
        }

        [HttpPost("{id:int}/close")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Close(int id)
        {
            var r = await _svc.CloseIPOAsync(id);
            return r.IsSuccess ? Ok(new { message = "IPO closed." }) : BadRequest(r.Error);
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyIPODto dto)
        {
            var r = await _svc.ApplyAsync(dto);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpGet("{id:int}/applications")]
        public async Task<IActionResult> GetApplications(int id)
        {
            var r = await _svc.GetApplicationsAsync(id);
            return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
        }

        [HttpGet("applications/{applicationId:int}")]
        public async Task<IActionResult> GetApplication(int applicationId)
        {
            var r = await _svc.GetApplicationAsync(applicationId);
            return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
        }

        [HttpPost("{id:int}/allocate")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Allocate(int id)
        {
            var r = await _svc.AllocateAsync(id);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpPost("{id:int}/refunds")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ProcessRefunds(int id)
        {
            var r = await _svc.ProcessRefundsAsync(id);
            return r.IsSuccess ? Ok(new { refundsProcessed = r.Value }) : BadRequest(r.Error);
        }
    }
}
