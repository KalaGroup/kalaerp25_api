using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FabricationController : ControllerBase
    {

        private readonly IFabrication _fabricationService;

        public FabricationController(IFabrication fabricationService)
        {
            _fabricationService = fabricationService;
        }


        [HttpGet("getCpyPrcddlFab")]
        public async Task<IActionResult> GetCpyPrcKVAFab(
            string pcCode, string machineCode, string kva, string model, string suppCode)
        {
            var result = await _fabricationService.GetCpyPrcddlFabAsync(
                pcCode, machineCode, kva, model, suppCode);

            return Ok(result);
        }

        [HttpGet("GetCpyKitFab")]
        public async Task<IActionResult> GetCpyKitFab(
    string pcCode, string machineCode, string planCode,
    string partCode, string cpyKit, string suppCode)
        {
            var result = await _fabricationService.GetCpyKitFabAsync(
                pcCode, machineCode, planCode, partCode, cpyKit, suppCode);

            return Ok(result);
        }

        [HttpGet("GetCpyKitDts")]
        public async Task<IActionResult> GetCpyKitDts(
string pcCode, int batchQty, string cpyKitCode, string bomCode, string pfbCode)
        {
            var result = await _fabricationService.CpyKitDtsAsync(
                pcCode, batchQty, cpyKitCode, bomCode, pfbCode);

            return Ok(result);
        }

        [HttpPost("FabricationSubmit")]
        public async Task<string> SubmitFabrication([FromBody] CpyPrcFabRequest cpyPrcFabReq, CancellationToken cancellationToken = default)
        {
            return await _fabricationService.SubmitFabricationAsync(cpyPrcFabReq, cancellationToken);
        }

        [HttpPost("FabricationCheckerSubmit")]
        public async Task<string> SubmitFabricationChecker([FromBody] CpyPrcFabCheckerRequest cpyPrcCheckerFabReq, CancellationToken ct = default)
        {
            return await _fabricationService.SubmitFabricationCheckerAsync(cpyPrcCheckerFabReq, ct);
        }




    }
}
