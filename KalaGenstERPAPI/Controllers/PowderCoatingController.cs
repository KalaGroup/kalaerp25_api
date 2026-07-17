using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PowderCoatingController : ControllerBase
    {
        private readonly IPowderCoating _powderCoating;

        public PowderCoatingController(IPowderCoating powderCoating)
        {
            _powderCoating = powderCoating;
        }
        [HttpGet("GetCpyKitPC")]
        public async Task<IActionResult> GetCpyKitPC(
         string pcCode, string machineCode, string planCode,
         string partCode, string cpyKit, string kva)
        {
            var result = await _powderCoating.GetCpyKitPCAsync(
                pcCode, machineCode, planCode, partCode, cpyKit, kva);

            return Ok(result);
        }

        [HttpPost("PowderCoatingSubmit")]
        public async Task<string> SubmitPowderCoating([FromBody] CpyPrcPCRequest cpyPrcPCReq, CancellationToken cancellationToken = default)
        {
            return await _powderCoating.SubmitPowderCoatingAsync(cpyPrcPCReq, cancellationToken);
        }

        [HttpPost("powdercoatingCheckerSubmit")]
        public async Task<string> SubmitPowderCoatingChecker([FromBody] CpyPrcPCCheckerRequest cpyPrcCheckerPCReq, CancellationToken ct = default)
        {
            return await _powderCoating.SubmitPowderCoatingCheckerAsync(cpyPrcCheckerPCReq, ct);
        }

    }
}
