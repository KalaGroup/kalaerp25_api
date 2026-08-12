using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DgStageCheckerController : ControllerBase
    {
        private readonly IDgStageChecker _dgStageChecker;
        public DgStageCheckerController(IDgStageChecker dgStageChecker)
        {
            _dgStageChecker = dgStageChecker;
        }
        // Route change — profitCenter is now OPTIONAL in the path (backward compat).
        // Two new query-string params are accepted:
        //   ?fromDate=2026-08-01&toDate=2026-08-11&lineWisePC=01.106
        // Any missing/blank value falls through to the SP's NULL default (no filter
        // for that dimension). The legacy 2-segment URL still works — old callers
        // will just pass the LineWisePC in the route slot and skip the date range.
        [HttpGet("GetStageQAPendingList/{stageName}/{profitCenter?}")]
        public async Task<IActionResult> GetStageQAPendingList(
            string stageName,
            string? profitCenter = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? lineWisePC = null)
        {
            try
            {
                // Prefer explicit ?lineWisePC=… when supplied; otherwise fall back to
                // the legacy /{profitCenter} path segment so old clients still work.
                var linePc = !string.IsNullOrWhiteSpace(lineWisePC) ? lineWisePC : profitCenter;

                var result = await _dgStageChecker.GetStageQAPendingListAsync(
                    stageName, fromDate, toDate, linePc);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetStageQualityDefect/{stageName}/{pccode}")]
        public async Task<IActionResult> GetQualityDefect(string stageName, string pccode)
        {
            try
            {
                var result = await _dgStageChecker.GetQualityDefectAsync(stageName, pccode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetDGAssemblyProfitcenters")]
        public async Task<IActionResult> GetDGAssemblyProfitcenters()
        {
            try
            {
                var result = await _dgStageChecker.GetDGAssemblyProfitcentersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GetActivePartKvaList, SaveStageWiseQualityCheckList, and
        // CheckDuplicateQualityCheckList have been moved to QualityController.

        // UpdateStageWiseQualityCheckList, SoftDeleteStageWiseQualityCheckList,
        // and GetAllQualityCheckLists have been moved to QualityController.

        [HttpGet("GetAllPendingAuthQualityList")]
        public async Task<IActionResult> GetAllPendingAuthQualityList()
        {
            try
            {
                var result = await _dgStageChecker.GetAllPendingAuthQualityListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetPendingAuthQAListDetails/{stageWiseQCId}")]
        public async Task<IActionResult> GetPendingAuthQAListDetails(int stageWiseQCId)
        {
            try
            {
                var result = await _dgStageChecker.GetPendingAuthQAListDetailsAsync(stageWiseQCId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("SaveOrUpdateQualityCheckpoint")]
        public async Task<IActionResult> SaveOrUpdateQualityCheckpoint([FromBody] SaveUpdateCheckpointRequest request)
        {
            try
            {
                await _dgStageChecker.SaveOrUpdateQualityCheckpointAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetStageAndKvaWiseCheckpointList/{stageName}/{pcCode}/{kva}")]
        public async Task<IActionResult> GetStageAndKvaWiseCheckpointList(string stageName, string pcCode, decimal kva)
        {
            try
            {
                var result = await _dgStageChecker.GetStageAndKvaWiseCheckpointListResponsesAsync(stageName, pcCode, kva);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("SaveQAStatusStagewise")]
        public async Task<IActionResult> SaveQAStatusStatuswise([FromBody] QualityProcessCheckerRequest request)
        {
            try
            {
                await _dgStageChecker.SaveQAStatusStagewiseAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("FetchEmployeeListToRaiseESP")]
        public async Task<IActionResult> FetchEmployeeList()
        {
            try
            {
                var result = await _dgStageChecker.FetchEmployeeListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("Select6MFromDB")]
        public Task<IActionResult> FetchDepartmentList()
        {
            try
            {
                var result = _dgStageChecker.Select6MFromDB();
                return Task.FromResult<IActionResult>(Ok(result));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(500, $"Internal server error: {ex.Message}"));
            }
        }
    }
}
