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
        [HttpGet("GetStageQAPendingList/{stageName}/{profitCenter}")]
        public async Task<IActionResult> GetStageQAPendingList(string stageName, string profitCenter)
        {
            try
            {
                var result = await _dgStageChecker.GetStageQAPendingListAsync(stageName, profitCenter);
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

        [HttpGet("GetActivePartKvaList")]
        public async Task<IActionResult> GetActivePartKvaList()
        {
            try
            {
                var result = await _dgStageChecker.GetActivePartKvaListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("SaveStageWiseQualityCheckList")]
        public async Task<IActionResult> SaveStageWiseQualityCheckList([FromBody] StageWiseQualityCheckListRequest request)
        {
            try
            {
                await _dgStageChecker.SaveStageWiseQualityCheckListAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("CheckDuplicateQualityCheckList/{pcCode}/{stageName}/{fromKva}/{toKva}")]
        public async Task<IActionResult> CheckDuplicateQualityCheckList(string pcCode, string stageName, string fromKva, string toKva)
        {
            try
            {
                var exists = await _dgStageChecker.CheckDuplicateQualityCheckListAsync(pcCode, stageName, fromKva, toKva);
                return Ok(new { isDuplicate = exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

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
