using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReverseController : ControllerBase
    {
        private readonly IReverse _reverseService;

        public ReverseController(IReverse reverseService)
        {
            _reverseService = reverseService;
        }

        [HttpGet("getRevPCCode")]
        public async Task<IActionResult> GetRevPCCode(string StrTransType, string CatId)
        {
            var rows = await _reverseService.GetRevPCCodeAsync(StrTransType, CatId);
            return Ok(rows);
        }

        [HttpGet("LoadPrcDts")]                              // → api/Reverse/LoadPrcDts
        public async Task<IActionResult> LoadRevPrcDts(string PCCode, string CatId)
        {
            var rows = await _reverseService.LoadRevPrcDtsAsync(PCCode, CatId);
            return Ok(rows);
        }


        [HttpPost("SubmitRevCpyTrans")]                 // → api/Reverse/SubmitRevCpyTrans
        public async Task<string> SubmitRevCpyTrans([FromBody] CpyRevRequest CpyRevReq, CancellationToken ct = default)
        {
            return await _reverseService.SubmitRevCpyTransAsync(CpyRevReq);
        }

    }
}
