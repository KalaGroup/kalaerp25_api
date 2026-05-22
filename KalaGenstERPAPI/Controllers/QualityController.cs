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
        private readonly string _fileBasePath;
        private readonly IWebHostEnvironment _env;

        public QualityController(IQuality qualityService, IWebHostEnvironment env, IConfiguration configuration)
        {
            _qualityService = qualityService;
            _env = env;
            // Base path for file storage, can be set in appsettings.json or environment variable
            _fileBasePath = configuration["FileStorage:BasePath"];
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
                    request.BeforePhotoPath = await SaveFile(beforePhoto, "before", request.CompanyId);
                    request.BeforePhotoName = beforePhoto.FileName;
                }
                if (afterPhoto != null)
                {
                    request.AfterPhotoPath = await SaveFile(afterPhoto, "after", request.CompanyId);
                    request.AfterPhotoName = afterPhoto.FileName;
                }
                if (impactGraph != null)
                {
                    request.ImpactGraphPath = await SaveFile(impactGraph, "graphs", request.CompanyId);
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

        private async Task<string> SaveFile(IFormFile file, string subFolder, string companyId)
        {
            // Get current year and month from server
            var now = DateTime.Now;
            var year = now.Year.ToString();                          // "2026"
            var month = now.ToString("MM") + " " + now.ToString("MMMM"); // "03 March"

            // Build path: F:\ERP\2026\03 March\Kaizen\before
            var basePath = @"D:\ERP";
            var uploadsFolder = Path.Combine(basePath, year, month, "Kaizen", subFolder);
            Directory.CreateDirectory(uploadsFolder);

            // Unique filename with companyCode: 07_20260311_143025_originalname.jpg
            var timestamp = now.ToString("yyyyMMdd_HHmmss");
            var safeFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var uniqueName = $"{companyId}_{timestamp}_{safeFileName}{extension}";

            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Return relative path for DB storage
            return Path.Combine("ERP", year, month, "Kaizen", subFolder, uniqueName).Replace("\\", "/");
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
                // New file uploaded → save and set path
                if (beforePhoto != null)
                {
                    request.BeforePhotoPath = await SaveFile(beforePhoto, "before", request.CompanyId);
                    request.BeforePhotoName = beforePhoto.FileName;
                }
                // else: request.BeforePhotoPath comes from FormData (existing path) or is null
                // The service will only overwrite if non-null/non-empty

                if (afterPhoto != null)
                {
                    request.AfterPhotoPath = await SaveFile(afterPhoto, "after", request.CompanyId);
                    request.AfterPhotoName = afterPhoto.FileName;
                }

                if (impactGraph != null)
                {
                    request.ImpactGraphPath = await SaveFile(impactGraph, "graphs", request.CompanyId);
                    request.ImpactGraphName = impactGraph.FileName;
                }

                var result = await _qualityService.UpdateKaizenSheet(id, request);
                return Ok(new { kaizenSheetNo = result, message = "Kaizen sheet updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to update Kaizen sheet", error = ex.Message });
            }
        }

        [HttpGet("GetKaizenFile")]
        public IActionResult GetKaizenFile([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path))
                return BadRequest(new { message = "File path is required" });

            // Build full path from ContentRootPath
            //var fullPath = Path.Combine(_fileBasePath, path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            var fullPath = Path.Combine(@"D:\", path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "File not found" });

            // Determine content type
            var ext = Path.GetExtension(fullPath).ToLower();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, contentType);
        }

        [HttpPut("AuthorizeKaizenSheet/{id}")]
        public async Task<IActionResult> AuthorizeKaizenSheet(int id)
        {
            try
            {
                var result = await _qualityService.AuthorizeKaizenSheet(id);
                if (result)
                {
                    return Ok(new { message = "Kaizen sheet authorized successfully." });
                }
                return NotFound(new { message = "Kaizen sheet not found or already authorized." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to authorize.", error = ex.Message });
            }
        }

        [HttpGet("GetAllQualityCheckLists")]
        public async Task<IActionResult> GetAllQualityCheckLists()
        {
            try
            {
                var data = await _qualityService.GetAllQualityCheckListsAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to fetch quality check lists.", Detail = ex.Message });
            }
        }

        [HttpPost("UpdateStageWiseQualityCheckList")]
        public async Task<IActionResult> UpdateStageWiseQualityCheckList([FromBody] UpdateStageWiseQualityCheckListRequest request)
        {
            if (request == null)
                return BadRequest(new { Message = "Request body is required." });
            if (request.stageWiseQcid <= 0)
                return BadRequest(new { Message = "stageWiseQcid is required." });

            try
            {
                var ok = await _qualityService.UpdateStageWiseQualityCheckListAsync(request);
                if (!ok)
                    return NotFound(new { Message = "Quality checklist not found." });
                return Ok(new { Message = "Quality checklist updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to update quality checklist.", Detail = ex.Message });
            }
        }

        [HttpGet("CheckDuplicateQualityCheckList/{pcCode}/{stageName}/{fromKva}/{toKva}")]
        public async Task<IActionResult> CheckDuplicateQualityCheckList(string pcCode, string stageName, string fromKva, string toKva)
        {
            try
            {
                var exists = await _qualityService.CheckDuplicateQualityCheckListAsync(pcCode, stageName, fromKva, toKva);
                return Ok(new { isDuplicate = exists });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to check duplicate quality check list.", Detail = ex.Message });
            }
        }

        [HttpPost("SaveStageWiseQualityCheckList")]
        public async Task<IActionResult> SaveStageWiseQualityCheckList([FromBody] StageWiseQualityCheckListRequest request)
        {
            if (request == null)
                return BadRequest(new { Message = "Request body is required." });

            try
            {
                await _qualityService.SaveStageWiseQualityCheckListAsync(request);
                return Ok(new { Message = "Quality checklist saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to save quality checklist.", Detail = ex.Message });
            }
        }

        [HttpPost("SoftDeleteStageWiseQualityCheckList/{id}")]
        public async Task<IActionResult> SoftDeleteStageWiseQualityCheckList(int id)
        {
            if (id <= 0)
                return BadRequest(new { Message = "id is required." });

            try
            {
                var ok = await _qualityService.SoftDeleteStageWiseQualityCheckListAsync(id);
                if (!ok)
                    return NotFound(new { Message = "Quality checklist not found." });
                return Ok(new { Message = "Quality checklist removed." });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to delete quality checklist.", Detail = ex.Message });
            }
        }

        [HttpPost("AuthorizeStageWiseQualityCheckList/{id}")]
        public async Task<IActionResult> AuthorizeStageWiseQualityCheckList(int id, [FromBody] AuthorizeQualityCheckListRequest request)
        {
            if (id <= 0)
                return BadRequest(new { Message = "id is required." });

            try
            {
                var ok = await _qualityService.AuthorizeStageWiseQualityCheckListAsync(id, request?.checkerRemark);
                if (!ok)
                    return NotFound(new { Message = "Quality checklist not found." });
                return Ok(new { Message = "Checklist authorized successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to authorize quality checklist.", Detail = ex.Message });
            }
        }

        [HttpPost("RevertAuthorizationStageWiseQualityCheckList/{id}")]
        public async Task<IActionResult> RevertAuthorizationStageWiseQualityCheckList(int id)
        {
            if (id <= 0)
                return BadRequest(new { Message = "id is required." });

            try
            {
                var ok = await _qualityService.RevertAuthorizationStageWiseQualityCheckListAsync(id);
                if (!ok)
                    return NotFound(new { Message = "Quality checklist not found." });
                return Ok(new { Message = "Authorization reverted." });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Failed to revert authorization.", Detail = ex.Message });
            }
        }
    }
}