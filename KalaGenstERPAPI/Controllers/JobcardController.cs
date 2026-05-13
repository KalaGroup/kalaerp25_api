using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Jobcard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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
        public async Task<IActionResult> GetJobCard1Details(string CompId, string AssemblyLine)
        {
            if (string.IsNullOrWhiteSpace(CompId))
                return BadRequest("CompId is required.");

            var data = await _jobcardService.GetDGAsync("DGWOP", CompId, AssemblyLine);

            if (data == null || data.Count == 0)
                return NotFound("No records found.");

            return Ok(data);
        }

        [HttpGet("GetJobCard2")]
        public async Task<IActionResult> GetJobCard2Details(string CompId, string AssemblyLine)
        {
            if (string.IsNullOrWhiteSpace(CompId))
                return BadRequest("CompId is required.");

            var data = await _jobcardService.GetJobCardDG2DtsAsync("DGWIP", CompId, AssemblyLine);

            if (data == null || data.Count == 0)
                return NotFound("No records found.");

            return Ok(data);
        }

        [HttpGet("GetCPDetails")]
        public async Task<IActionResult> GetCPDetails()
        {         
            var data = await _jobcardService.GetJobCard2CPAsync();
            if (data == null || data.Count == 0)
                return NotFound("No records found.");
            return Ok(data);
        }

        [HttpGet("GetCPStk")]
        public async Task<IActionResult> GetCPStkDetails(string strKVA, string ph, string panelType, string compId, string assemblyLine)
        {
            var data = await _jobcardService.GetCPStkAsync(strKVA, ph, panelType, compId, assemblyLine);
            if (string.IsNullOrEmpty(data))
                return NotFound("No records found.");
            return Ok(data);
        }

        [HttpPost("SubmitJobCard")]
        public async Task<IActionResult> SubmitJobCard([FromBody] JobCardSubmitRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            try
            {
                var result = await _jobcardService.SubmitJobCardAsync(request);

                if (string.IsNullOrWhiteSpace(result))
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to submit job card.");

                // Service returns "JCD/..." (one or more job card codes joined by '#') on success.
                // Anything else is a business-rule message (e.g. "Engine SrNo Not available For DG ...").
                if (!result.StartsWith("JCD/"))
                    return BadRequest(result);

                return Ok(result);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                Console.Error.WriteLine($"[SubmitJobCard] Duplicate key: {ex.Message}");
                return Conflict("This job card has already been created for the selected plan. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SubmitJobCard] Unhandled exception: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Failed to submit job card. Please try again or contact support.");
            }
        }

        [HttpGet("GetJobCard1CheckerDetails")]
        public async Task<IActionResult> GetJobCard1ChekerDetails()
        {
            var data = await _jobcardService.GetPendingAuthJobCodes();
            if (data == null || data.Count == 0)
                return NotFound("No records found.");
            return Ok(data);
        }

        // GET api/Jobcard/GetJobCard2Report?CompanyCode=03&AssemblyLine=03.123&FromDate=2026-02-01&ToDate=2026-02-28
        [HttpGet("GetJobCard2Report")]
        public async Task<IActionResult> GetJobCard2Report(
            [FromQuery] string CompanyCode,
            [FromQuery] string AssemblyLine,
            [FromQuery] DateTime FromDate,
            [FromQuery] DateTime ToDate)
        {
            if (string.IsNullOrWhiteSpace(CompanyCode))
                return BadRequest("CompanyCode is required.");
            if (string.IsNullOrWhiteSpace(AssemblyLine))
                return BadRequest("AssemblyLine is required.");
            if (FromDate == default)
                return BadRequest("FromDate is required.");
            if (ToDate == default)
                return BadRequest("ToDate is required.");
            if (FromDate > ToDate)
                return BadRequest("FromDate cannot be later than ToDate.");

            var data = await _jobcardService.GetJobCard2ReportAsync(
                CompanyCode.Trim(), AssemblyLine.Trim(), FromDate, ToDate);

            if (data == null || data.Count == 0)
                return NotFound("No records found for the specified filters.");

            return Ok(data);
        }

        // GET api/Jobcard/GetJobCard1Report?CompanyCode=03&AssemblyLine=03.123&FromDate=2026-04-01&ToDate=2026-05-06
        [HttpGet("GetJobCard1Report")]
        public async Task<IActionResult> GetJobCard1Report(
            [FromQuery] string CompanyCode,
            [FromQuery] string AssemblyLine,
            [FromQuery] DateTime FromDate,
            [FromQuery] DateTime ToDate)
        {
            if (string.IsNullOrWhiteSpace(CompanyCode))
                return BadRequest("CompanyCode is required.");
            if (string.IsNullOrWhiteSpace(AssemblyLine))
                return BadRequest("AssemblyLine is required.");
            if (FromDate == default)
                return BadRequest("FromDate is required.");
            if (ToDate == default)
                return BadRequest("ToDate is required.");
            if (FromDate > ToDate)
                return BadRequest("FromDate cannot be later than ToDate.");

            var data = await _jobcardService.GetJobCard1ReportAsync(
                CompanyCode.Trim(), AssemblyLine.Trim(), FromDate, ToDate);

            if (data == null || data.Count == 0)
                return NotFound("No records found for the specified filters.");

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
