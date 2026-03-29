using System;
using System.Threading.Tasks;
using BdStockOMS.API.DTOs.TBond;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/tbond")]
    [Authorize]
    public class TBondController : ControllerBase
    {
        private readonly ITBondService _svc;
        public TBondController(ITBondService svc) => _svc = svc;

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTBondDto dto)
        {
            var r = await _svc.CreateAsync(dto);
            return r.IsSuccess ? CreatedAtAction(nameof(Get), new { id = r.Value!.Id }, r.Value) : BadRequest(r.Error);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var r = await _svc.GetAsync(id);
            return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
            => Ok((await _svc.GetAllAsync(status)).Value);

        [HttpPost("orders")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceTBondOrderDto dto)
        {
            var r = await _svc.PlaceOrderAsync(dto);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] int? investorId, [FromQuery] int? tbondId)
            => Ok((await _svc.GetOrdersAsync(investorId, tbondId)).Value);

        [HttpPost("orders/{id:int}/execute")]
        [Authorize(Roles = "SuperAdmin,Admin,Trader")]
        public async Task<IActionResult> ExecuteOrder(int id)
        {
            var r = await _svc.ExecuteOrderAsync(id);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpPost("orders/{id:int}/settle")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> SettleOrder(int id)
        {
            var r = await _svc.SettleOrderAsync(id);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpPost("orders/{id:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var r = await _svc.CancelOrderAsync(id);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpPost("{id:int}/coupons/generate")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GenerateCoupons(int id)
        {
            var r = await _svc.GenerateCouponsAsync(id);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpGet("{id:int}/coupons")]
        public async Task<IActionResult> GetCoupons(int id)
        {
            var r = await _svc.GetCouponsAsync(id);
            return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
        }

        [HttpPost("{id:int}/coupons/pay")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> PayCoupons(int id, [FromQuery] DateTime upTo)
        {
            var r = await _svc.PayCouponsAsync(id, upTo);
            return r.IsSuccess ? Ok(new { couponsPaid = r.Value }) : BadRequest(r.Error);
        }

        [HttpPost("{id:int}/mature")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ProcessMaturity(int id)
        {
            var r = await _svc.ProcessMaturityAsync(id);
            return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
        }

        [HttpGet("holdings/{investorId:int}")]
        public async Task<IActionResult> GetHoldings(int investorId)
            => Ok((await _svc.GetHoldingsAsync(investorId)).Value);
    }
}
