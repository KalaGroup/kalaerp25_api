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

        [HttpGet("GetJobCard1CheckerDetails")]
        public async Task<IActionResult> GetJobCard1ChekerDetails()
        {
            var data = await _jobcardService.GetPendingAuthJobCodes();
            if (data == null || data.Count == 0)
                return NotFound("No records found.");
            return Ok(data);
        }

        [HttpGet("GetJobCard1CheckerDetails/{jobCode}")]
        public async Task<IActionResult> GetJobCard1CheckerDetails(string jobCode)
        {
            if (string.IsNullOrWhiteSpace(jobCode))
                return BadRequest("jobCode is required.");
            jobCode = Uri.UnescapeDataString(jobCode);
            var data = await _jobcardService.GetDGJobcard1CheckerDetails(jobCode);
            if (data == null || data.Count == 0)
                return NotFound("No records found for the specified job code.");
            return Ok(data);

        }

        [HttpGet("GetPlanDetails/{jobCode}")]
        public async Task<IActionResult> GetPlanDetails(string jobCode)
        {
            if (string.IsNullOrWhiteSpace(jobCode))
                return BadRequest("jobCode is required.");
            jobCode = Uri.UnescapeDataString(jobCode);
            var data = await _jobcardService.GetPlanDetails(jobCode);
            if (data == null || data.Count == 0)
                return NotFound("No records found for the specified job code.");
            return Ok(data);
        }

        [HttpPost("SubmitJobcard1Checker")]
        public async Task<IActionResult> SubmitJobcard1Checker([FromBody] Jobcard1CheckerSubmitRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            var result = await _jobcardService.SubmitJobcard1Checker(request);
            if (string.IsNullOrEmpty(result))
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to submit job card checker.");
            return Ok(result);
        }
    }
}
