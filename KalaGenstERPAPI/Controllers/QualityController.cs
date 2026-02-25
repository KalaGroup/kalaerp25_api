//using KalaGenset.ERP.Core.Interface;
//using KalaGenset.ERP.Core.Request;
//using KalaGenset.ERP.Core.ResponseDTO;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace KalaGenset.ERP.API.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class QualityController : ControllerBase
//    {
//        private readonly IQuality _qualityService;
//        public QualityController(IQuality qualityService)
//        {
//            _qualityService = qualityService;
//        }

//        [HttpGet("GetCompany")]
//        public async Task<IActionResult> GetCompanyFromDB()
//        {
//            var result = _qualityService.FetchCompanyDetails();
//            return Ok(result);
//        }

//        [HttpGet("GetPCNames")]
//        public async Task<IActionResult> GetPCNamesFromDB()
//        {
//            var result = _qualityService.FetchPCNames();
//            return Ok(result);
//        }

//        [HttpGet("GetPartcodesForCalibration")]
//        public async Task<IActionResult> GetPartcodesForCalibrationFromDB()
//        {
//            var result = _qualityService.FetchPartcodesForCalibration();
//            return Ok(result);
//        }

//        [HttpPost("SaveCalibrationMaster")]
//        public async Task<IActionResult> SaveCalibrationMaster([FromBody] CalibrationMasterRequest request)
//        {
//            var result = await _qualityService.SaveCalibrationMasterAsync(request);
//            if (result)
//            {
//                return Ok(new { Message = "Calibration master saved successfully." });
//            }
//            else
//            {
//                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to save calibration master." });
//            }
//        }

//        [HttpGet("GetUnauthorizedCalibrationData/{companyId}")]
//        public async Task<IActionResult> GetUnauthorizedCalibrationData(int companyId)
//        {
//            var result = await _qualityService.GetUnauthorizedCalibrationDataAsync(companyId);
//            return Ok(result);
//        }

//        [HttpGet("GetDivisionCodeAndName")]
//        public async Task<IActionResult> GetDivisionCodeAndName()
//        {
//            var result = await _qualityService.GetDivisonCOdeAndNameFromDB();
//            return Ok(result);
//        }

//        [HttpGet("GetDepartmentsByDivisionId/{divisionId}")]
//        public async Task<IActionResult> GetDepartmentsByDivisionCode(int divisionId)
//        {
//            var result = await _qualityService.GetDepartmentsByDivisionCodeAsync(divisionId);
//            return Ok(result);
//        }

//        [HttpGet("GetWorkstationCodeAndName")]
//        public async Task<IActionResult> GetWorkstationNameAndName()
//        {
//            var result = await _qualityService.GetWorkstationsByDepartmentCodeAsync();
//            return Ok(result);
//        }

//        [HttpPost("SaveKaizenSheet")]
//        public async Task<IActionResult> SaveKaizenSheet([FromForm] CreateKaizenSheetRequest request, IFormFile? beforePhoto, IFormFile? afterPhoto, IFormFile? impactGraph)
//        {
//            try
//            {
//                // Save files in controller (has access to IWebHostEnvironment)
//                if (beforePhoto != null)
//                {
//                    request.BeforePhotoPath = await SaveFile(beforePhoto, "before");
//                    request.BeforePhotoName = beforePhoto.FileName;
//                }
//                if (afterPhoto != null)
//                {
//                    request.AfterPhotoPath = await SaveFile(afterPhoto, "after");
//                    request.AfterPhotoName = afterPhoto.FileName;
//                }
//                if (impactGraph != null)
//                {
//                    request.ImpactGraphPath = await SaveFile(impactGraph, "graphs");
//                    request.ImpactGraphName = impactGraph.FileName;
//                }

//                var result = await _qualityService.CreateKaizenSheet(request);
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { message = "Failed to save Kaizen sheet", error = ex.Message });
//            }
//        }

//        private async Task<string> SaveFile(IFormFile file, string subFolder)
//        {
//            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "kaizen", subFolder);
//            Directory.CreateDirectory(uploadsFolder);
//            var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
//            var filePath = Path.Combine(uploadsFolder, uniqueName);
//            using var stream = new FileStream(filePath, FileMode.Create);
//            await file.CopyToAsync(stream);
//            return $"/uploads/kaizen/{subFolder}/{uniqueName}";
//        }
//    }
//}


using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using KalaGenset.ERP.Core.ResponseDTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QualityController : ControllerBase
    {
        private readonly IQuality _qualityService;
        private readonly IWebHostEnvironment _env;

        public QualityController(IQuality qualityService, IWebHostEnvironment env)
        {
            _qualityService = qualityService;
            _env = env;
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

        [HttpGet("GetDivisionCodeAndName")]
        public async Task<IActionResult> GetDivisionCodeAndName()
        {
            var result = await _qualityService.GetDivisonCOdeAndNameFromDB();
            return Ok(result);
        }

        [HttpGet("GetDepartmentsByDivisionId/{divisionId}")]
        public async Task<IActionResult> GetDepartmentsByDivisionCode(int divisionId)
        {
            var result = await _qualityService.GetDepartmentsByDivisionCodeAsync(divisionId);
            return Ok(result);
        }

        [HttpGet("GetWorkstationCodeAndName")]
        public async Task<IActionResult> GetWorkstationNameAndName()
        {
            var result = await _qualityService.GetWorkstationsByDepartmentCodeAsync();
            return Ok(result);
        }

        [HttpPost("SaveKaizenSheet")]
        public async Task<IActionResult> SaveKaizenSheet(
            [FromForm] CreateKaizenSheetRequest request,
            IFormFile? beforePhoto,
            IFormFile? afterPhoto,
            IFormFile? impactGraph)
        {
            try
            {
                if (beforePhoto != null)
                {
                    request.BeforePhotoPath = await SaveFile(beforePhoto, "before");
                    request.BeforePhotoName = beforePhoto.FileName;
                }
                if (afterPhoto != null)
                {
                    request.AfterPhotoPath = await SaveFile(afterPhoto, "after");
                    request.AfterPhotoName = afterPhoto.FileName;
                }
                if (impactGraph != null)
                {
                    request.ImpactGraphPath = await SaveFile(impactGraph, "graphs");
                    request.ImpactGraphName = impactGraph.FileName;
                }

                var kaizenSheetNo = await _qualityService.CreateKaizenSheet(request);
                return Ok(new { kaizenSheetNo = kaizenSheetNo, message = "Kaizen sheet created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to save Kaizen sheet", error = ex.Message });
            }
        }

        private async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            // ContentRootPath is NEVER null (unlike WebRootPath)
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Kaizen", subFolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/Uploads/Kaizen/{subFolder}/{uniqueName}";
        }

        [HttpGet("GetAllKaizenSheets")]
        public async Task<IActionResult> GetAllKaizenSheets()
        {
            var result = await _qualityService.GetAllKaizenSheetsFull();
            return Ok(result);
        }

        [HttpDelete("DeleteKaizenSheet/{id}")]
        public async Task<IActionResult> DeleteKaizenSheet(int id)
        {
            var result = await _qualityService.DeleteKaizenSheet(id);
            if (result)
                return Ok(new { message = "Kaizen sheet deleted successfully" });
            return NotFound(new { message = "Kaizen sheet not found" });
        }

        [HttpPut("UpdateKaizenSheet/{id}")]
        public async Task<IActionResult> UpdateKaizenSheet(
            int id,
            [FromForm] CreateKaizenSheetRequest request,
            IFormFile? beforePhoto,
            IFormFile? afterPhoto,
            IFormFile? impactGraph)
        {
            try
            {
                if (beforePhoto != null)
                {
                    request.BeforePhotoPath = await SaveFile(beforePhoto, "before");
                    request.BeforePhotoName = beforePhoto.FileName;
                }
                if (afterPhoto != null)
                {
                    request.AfterPhotoPath = await SaveFile(afterPhoto, "after");
                    request.AfterPhotoName = afterPhoto.FileName;
                }
                if (impactGraph != null)
                {
                    request.ImpactGraphPath = await SaveFile(impactGraph, "graphs");
                    request.ImpactGraphName = impactGraph.FileName;
                }

                var result = await _qualityService.UpdateKaizenSheet(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to update Kaizen sheet", error = ex.Message });
            }
        }
    }
}