using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QualityController : ControllerBase
    {
        private readonly IQuality _qualityService;
        public QualityController(IQuality qualityService)
        {
            _qualityService = qualityService;
        }

        [HttpGet("GetCompany")]
        public async Task<IActionResult> GetCompanyFromDB()
        {
            var result = _qualityService.FetchCompanyDetails();
            return Ok(result);
        }

        [HttpGet("GetPCNames")]
        public async Task<IActionResult> GetPCNamesFromDB()
        {
            var result = _qualityService.FetchPCNames();
            return Ok(result);
        }

        [HttpGet("GetPartcodesForCalibration")]
        public async Task<IActionResult> GetPartcodesForCalibrationFromDB()
        {
            var result = _qualityService.FetchPartcodesForCalibration();
            return Ok(result);
        }

        [HttpPost("SaveCalibrationMaster")]
        public async Task<IActionResult> SaveCalibrationMaster([FromBody] CalibrationMasterRequest request)
        {
            var result = await _qualityService.SaveCalibrationMasterAsync(request);
            if (result)
            {
                return Ok(new { Message = "Calibration master saved successfully." });
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to save calibration master." });
            }
        }

        [HttpGet("GetUnauthorizedCalibrationData/{companyId}")]
        public async Task<IActionResult> GetUnauthorizedCalibrationData(int companyId)
        {
            var result = await _qualityService.GetUnauthorizedCalibrationDataAsync(companyId);
            return Ok(result);
        }
    }
}
