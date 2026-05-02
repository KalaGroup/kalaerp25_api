using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.Request.Logistic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogisticController : ControllerBase
    {
        private readonly ILogistic _logisticService;
        public LogisticController(ILogistic logisticService)
        {
            _logisticService = logisticService;
        }

        [HttpGet("GetPCodeAll/{PCCode}/{ReqType}")]
        public async Task<IActionResult> GetPCodeAll(string PCCode, string ReqType)
        {
            var result = await _logisticService.GetPCodeAllAsync(PCCode, ReqType);
            return Ok(result);
        }

        [HttpGet("GetMTFCode/{FPCCode}/{TPCCode}")]
        public async Task<IActionResult> GetMTFCode(string FPCCode, string TPCCode)
        {
            var result = await _logisticService.GetMTFCodeAndMTFNoAsync(FPCCode, TPCCode);
            return Ok(result);
        }

        [HttpGet("GetReqCodeForMTF/{FPCCode}/{TPCCode}")]
        public async Task<IActionResult> GetReqCodeForMTF(string FPCCode, string TPCCode)
        {
            var result = await _logisticService.GetReqCodeForMTFAsync(FPCCode, TPCCode);
            return Ok(result);
        }

        [HttpGet("GetPartDescByMTFCode/{MTFCode}")]
        public async Task<IActionResult> GetPartDescByMTFCode(string MTFCode)
        {
            var result = await _logisticService.GetReqProductDtlAsync(MTFCode);
            return Ok(result);
        }

        [HttpGet("GetMTFSrNoDtl/{MTFCode}")]
        public async Task<IActionResult> GetMTFSrNoDtl(string MTFCode)
        {
            var result = await _logisticService.GetMTFSrNoDtlAsync(MTFCode);
            return Ok(result);
        }

        [HttpPost("SubmitMTFScanDetails")]
        public async Task<IActionResult> SubmitMTFScanDetails([FromBody] MTFScanSubmitRequest mtfScanSubmitRequest)
        {
            var result = await _logisticService.SubmitMTFScanDetailsAsync(mtfScanSubmitRequest);
            return Ok();
        }

        [HttpPost("GetReqDetails")]
        public async Task<IActionResult> GetReqDetails([FromBody] GetReqDetailsRequest request)
        {
            var result = await _logisticService.GetReqDetailsAsync(
                request.PCCode,
                request.StrBomCode,
                request.StrReqCode,
                request.StrReqQty,
                request.StrMTFQty);
            return Ok(result);
        }

        [HttpPost("SubmitMTFWipInternal")]
        public async Task<IActionResult> SubmitMTFWipInternal([FromBody] MTFWIPInternalRequest req)
        {
            var result = await _logisticService.SubmitMTFWIPInternalAsync(req);
            return Ok(result);

        }
    }
}
