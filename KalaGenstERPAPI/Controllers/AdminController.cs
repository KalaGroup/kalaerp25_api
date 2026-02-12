using KalaGenset.ERP.Core.DTO;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : Controller
    {

        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("UserRights/{PCID}")]
        public IActionResult GetUserRightsBYPCID(int PCID)
        {
            var result = _adminService.GetProfitCenterPermissions(PCID);
            return Ok(result);
        }

        [HttpPut("SaveUserRights")]
        public async Task<IActionResult> SaveUserRights([FromBody] List<UserRightsInputDTO> pageData)
        {
            if (pageData == null || !pageData.Any())
            {
                return BadRequest("No data received.");
            }
            else
            {
                var result = await _adminService.SaveUserRightsAsync(pageData);

                return Ok(new { message = "Role permissions updated successfully." });
            }

        }

        [HttpGet("GetEmployee")]
        public ActionResult<IEnumerable<EmployeeDTO>> GetEmployees()
        {
            var employees = _adminService.FetchEmployees();
            return Ok(employees);
        }

        [HttpGet("GetRoleByMapProfitCenterId/{profitCenterId}")]
        public ActionResult<IEnumerable<EmployeeDTO>> GetRoles(int profitCenterId)
        {
            var roles = _adminService.FetchRoles(profitCenterId);

            if (roles == null || !roles.Any())
            {
                return NotFound("No roles found for the provided PCID.");
            }

            return Ok(roles);
        }


        [HttpGet("GetCompanyDetails")]
        public ActionResult<IEnumerable<Core.DTO.CompanyDTO>> GetCompanyDetails()
        { 
          var companies= _adminService.FetchCompanyDetails();
            return Ok(companies);
        }

        [HttpGet("GetProfitCenterDetails/{CompanyId}")]
        public ActionResult<IEnumerable<ProfitCenterDTO>> GetProfitCenterDetails(int CompanyId)
        { 
          var profitcenters= _adminService.FetchProfitCenterDetails(CompanyId);
            return Ok(profitcenters);
        }

        [HttpGet("GetProfitCentersDetails")]
        public IActionResult GetProfitCenters()
        {
            var profitCenters = _adminService.GetProfitCentersDetails();

            if (profitCenters == null || !profitCenters.Any())
            {
                return NotFound("No Profit Centers found.");
            }

            return Ok(profitCenters);
        }

        //[HttpPost("api/profitcenters/bycompanycode")]
        //public IActionResult GetProfitCentersByCompanyCode([FromBody] string[] companyCodes)
        //{
        //    if (companyCodes == null || companyCodes.Length == 0)
        //    {
        //        return BadRequest("Company codes are required.");
        //    }

        //    var profitCenters = _adminService.FetchProfitCentersByCompanyCode(companyCodes);

        //    if (profitCenters == null || !profitCenters.Any())
        //    {
        //        return NotFound("No profit centers found for the provided company codes.");
        //    }

        //    return Ok(profitCenters);
        //}

        [HttpPost("InsertUserRoleInfo")]
        public async Task<IActionResult> InsertUserRoleInfo([FromBody] UserRoleDTO userDto)
        {
            if (userDto == null || userDto.EmpId <= 0)
            {
                return BadRequest("Invalid user data.");
            }

            try
            {
                int recordsInserted = await _adminService.InsertUserRoleInfo(userDto);
                return Ok(new { Message = $"{recordsInserted} records inserted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //[HttpPost("add-employee")]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> AddEmployee([FromForm] EmpMstDTO employeeDto, IFormFile? PhotoCopy)
        //{
        //    byte[]? photoCopyBytes = null;

        //    if (PhotoCopy != null)
        //    {
        //        using var memoryStream = new MemoryStream();
        //        await PhotoCopy.CopyToAsync(memoryStream);
        //        photoCopyBytes = memoryStream.ToArray();
        //    }

        //    var result = await _adminService.AddEmployeeAsync(employeeDto, photoCopyBytes);
        //    return Ok(result);
        //}

        [HttpGet("GetRoleByMapCompanyIdAndProfitCenterId/{selectedCompanyId}/{selectedDepartmentId}")]
        public IActionResult GetRolesByPCIDAndCID(int selectedCompanyId, int selectedDepartmentId)
        {
            var roles = _adminService.GetRolesByProfitCenterIdAndCompanyId(selectedCompanyId, selectedDepartmentId);

            if (roles == null || !roles.Any())
            {
                return NotFound("No roles found for the provided PCID.");
            }

            return Ok(roles);
        }

        [HttpGet("GetPermittedPages/{pcId}/{roleId}")]
        public async Task<IActionResult> GetPermittedPages(int pcId, int roleId)
        {
            var pages = await _adminService.GetPermittedPages(pcId, roleId);
            return Ok(pages);
        }

        [HttpPost("UpdatePagePermissions")]
        public async Task<IActionResult> UpdatePagePermissions([FromBody] List<PagePermissionUpdateDto> updates)
        {
            if (updates == null || updates.Count == 0)
            {
                return BadRequest(new { message = "No updates provided" });
            }

            var result = await _adminService.UpdatePagePermissionsAsync(updates);

            if (result)
            {
                return Ok(new { message = "Page Permissions Updated Successfully...!" });
            }

            return BadRequest(new { message = "Failed to update page permissions" });
        }


    }
}
