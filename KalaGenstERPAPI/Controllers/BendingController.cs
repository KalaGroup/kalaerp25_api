using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.ResponseDTO.Bending;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BendingController : ControllerBase
    {
        private readonly IBending _bendingService;

        public BendingController(IBending bendingService)
        {
            _bendingService = bendingService;
        }

        [HttpGet("GetCpyKit")]
        public async Task<ActionResult<DataTable>> GetCpyKit([FromQuery] string pcCode,[FromQuery] string machineCode,[FromQuery] string planCode,[FromQuery] string partCode,[FromQuery] string cpyKit,CancellationToken cancellationToken)
        {
            var result = await _bendingService.GetCpyKitAsync(
                pcCode, machineCode, planCode, partCode, cpyKit, cancellationToken);

            return Ok(result);
        }

        [HttpGet]
        [Route("GetCpyKitDts")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetCpyKitDts([FromQuery] string PCCode,[FromQuery] int BatchQty,[FromQuery] string CpyKitcode,[FromQuery] string BOMCode,[FromQuery] string PFBCode, CancellationToken cancellationToken)
        {
            var result = await _bendingService.GetCpyKitDtsAsync(
                PCCode, BatchQty, CpyKitcode, BOMCode, PFBCode, cancellationToken);

            return Ok(result);
        }

        [HttpPost("BendingSubmit")]
        public async Task<ActionResult<string>> BendingSubmit([FromBody] CpyPrcBendRequest request,CancellationToken cancellationToken)
        {
            var result = await _bendingService.SubmitBendingAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("Bending_chekerSave")]      // <-- matches the action segment
        public async Task<string> SubmitBendingChecker([FromBody] CpyPrcBendCheckerRequest CpyPrcbenReq,CancellationToken ct)
        {
            return await _bendingService.SubmitBendingCheckerAsync(CpyPrcbenReq, ct);
        }

    }
}
