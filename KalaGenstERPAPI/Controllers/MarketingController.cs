using AutoMapper;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketingController : ControllerBase
    {
        private readonly IMarketing _marketing;
        public MarketingController(IMarketing marketing)
        {
            _marketing = marketing;
        }

        [HttpGet("GetPendingMOFNFA")]
        public async Task<IActionResult> GetPendingMOFNFA()
        {
            var result = await _marketing.GetPendingMOFNFAsync();

            if (result == null || result.Count == 0)
                return NotFound("No data found.");

            return Ok(result);
        }

        [HttpPost("SubmitMOFNFALevel")]
        public async Task<IActionResult> SubmitMOFNFALevel([FromBody] MOFNFALevelAuthSubmitRequest req)
        {
            var result = await _marketing.SubmitMOFNFALevelAsync(req);
            if (string.IsNullOrEmpty(result))
                return BadRequest("Submission failed.");
            return Ok(result);
        }
    }
}
