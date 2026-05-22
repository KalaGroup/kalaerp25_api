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

        [HttpGet("GetReverseTransMst")]
        public async Task<IActionResult> GetReverseTransMst(string pcCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");

            var data = await _jobcardService.GetReverseTransMstAsync(pcCode);

            if (data == null || data.Count == 0)
                return NotFound("No records found.");

            return Ok(data);
        }

        [HttpGet("GetReverseKvaList")]
        public async Task<IActionResult> GetReverseKvaList(int transType, string pcCode)
        {
            if (transType <= 0)
                return BadRequest("transType is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");

            var data = await _jobcardService.GetReverseKvaListAsync(transType, pcCode);

            if (data == null || data.Count == 0)
                return NotFound("No records found.");

            return Ok(data);
        }

        [HttpGet("GetReverseModelList")]
        public async Task<IActionResult> GetReverseModelList(int transType, string pcCode, string kva)
        {
            if (transType <= 0)
                return BadRequest("transType is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            if (string.IsNullOrWhiteSpace(kva))
                return BadRequest("kva is required.");

            var data = await _jobcardService.GetReverseModelListAsync(transType, pcCode, kva);

            if (data == null || data.Count == 0)
                return NotFound("No records found.");

            return Ok(data);
        }

        [HttpGet("GetRevTransDts")]
        public async Task<IActionResult> GetRevTransDts(int transType, string pcCode, string kva, string model)
        {
            if (transType <= 0)
                return BadRequest("transType is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            if (string.IsNullOrWhiteSpace(kva))
                return BadRequest("kva is required.");
            if (string.IsNullOrWhiteSpace(model))
                return BadRequest("model is required.");

            var data = await _jobcardService.GetRevTransDtsAsync(transType, pcCode, kva, model);

            if (data == null || data.Count == 0)
                return NotFound("No records found.");

            return Ok(data);
        }

        [HttpPost("SubmitReverseTrans")]
        public async Task<IActionResult> SubmitReverseTrans([FromBody] ReverseTransRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var result = await _jobcardService.SubmitReverseTransAsync(request);

            if (string.IsNullOrEmpty(result))
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to submit reverse transaction.");

            // Service returns "StackTrace ... Message ..." on exception path —
            // surface as 500 so the client can distinguish success from failure.
            if (result.StartsWith("StackTrace"))
                return StatusCode(StatusCodes.Status500InternalServerError, result);

            return Ok(result);
        }
    }
}
