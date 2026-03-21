using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Jobcard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobcardController : ControllerBase
    {
        private readonly IJobcard _jobcardService;

        public JobcardController(IJobcard jobcard)
        {
            _jobcardService = jobcard;
        }

        [HttpGet("GetJobCard1")]
        public async Task<IActionResult> GetJobCard1Details(string CompId)
        {
            if (string.IsNullOrWhiteSpace(CompId))
                return BadRequest("CompId is required.");

            var data = await _jobcardService.GetDGAsync("DGWOP", CompId);

            if (data == null || data.Count == 0)
                return NotFound("No records found.");

            return Ok(data);
        }

        [HttpPost("SubmitJobCard")]
        public async Task<IActionResult> SubmitJobCard([FromBody] JobCardSubmitRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            var result = await _jobcardService.SubmitJobCardAsync(request);
            if (string.IsNullOrEmpty(result))
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to submit job card.");
            return Ok(result);
        }
    }
}
