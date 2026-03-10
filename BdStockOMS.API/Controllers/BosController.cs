using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BosController : ControllerBase
    {
        private readonly IBosXmlService _bosService;

        public BosController(IBosXmlService bosService)
        {
            _bosService = bosService;
        }

        // POST api/bos/upload/clients
        [HttpPost("upload/clients")]
        [Authorize(Roles = "SuperAdmin,Admin,Compliance")]
        public async Task<IActionResult> UploadClients([FromBody] BosUploadRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var result = await _bosService.ReconcileClientsAsync(request);

            if (result.Status == "Failed")
                return UnprocessableEntity(result);

            return Ok(result);
        }

        // POST api/bos/upload/positions
        [HttpPost("upload/positions")]
        [Authorize(Roles = "SuperAdmin,Admin,Compliance")]
        public async Task<IActionResult> UploadPositions([FromBody] BosUploadRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var result = await _bosService.ReconcilePositionsAsync(request);

            if (result.Status == "Failed")
                return UnprocessableEntity(result);

            return Ok(result);
        }

        // GET api/bos/sessions/{brokerageHouseId}
        [HttpGet("sessions/{brokerageHouseId:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Compliance")]
        public async Task<IActionResult> GetSessions(int brokerageHouseId)
        {
            var sessions = await _bosService.GetSessionsAsync(brokerageHouseId);
            return Ok(sessions);
        }

        // GET api/bos/export/positions/{brokerageHouseId}
        [HttpGet("export/positions/{brokerageHouseId:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Compliance")]
        public async Task<IActionResult> ExportPositions(int brokerageHouseId)
        {
            var result = await _bosService.ExportPositionsToXmlAsync(brokerageHouseId);
            return Ok(result);
        }

        // GET api/bos/verify-md5
        [HttpPost("verify-md5")]
        [Authorize(Roles = "SuperAdmin,Admin,Compliance")]
        public IActionResult VerifyMd5([FromBody] Md5VerifyRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var actual   = _bosService.ComputeMd5(request.FileContent);
            var verified = _bosService.VerifyMd5(request.FileContent, request.ExpectedMd5);

            return Ok(new
            {
                verified,
                actual,
                expected = request.ExpectedMd5
            });
        }
    }

    public class Md5VerifyRequest
    {
        public string FileContent { get; set; } = string.Empty;
        public string ExpectedMd5 { get; set; } = string.Empty;
    }
}
