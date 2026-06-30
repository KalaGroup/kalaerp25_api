using System;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialController : ControllerBase
    {
        private readonly IMaterial _service;

        public MaterialController(IMaterial service)
        {
            _service = service;
        }

        // GET api/Material/GetDepartments?companyCode=01
        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments(string companyCode)
        {
            var result = await _service.GetDepartmentsAsync(companyCode);
            return Ok(result);
        }

        // GET api/Material/GetMaterialRecords?companyCode=01&date=2026-06-24&deptCode=01.098
        [HttpGet("GetMaterialRecords")]
        public async Task<IActionResult> GetMaterialRecords(string companyCode, DateTime? date, string? deptCode)
        {
            var result = await _service.GetMaterialRecordsAsync(companyCode, date, deptCode);
            return Ok(result);
        }

        // POST api/Material/SaveMaterialBatch
        [HttpPost("SaveMaterialBatch")]
        public async Task<IActionResult> SaveMaterialBatch([FromBody] MaterialBatchRequest request)
        {
            var result = await _service.SaveMaterialBatchAsync(request);
            return Ok(new { success = result });
        }

        // DELETE api/Material/DeleteMaterialRecord?mcode=MATL/26-27/01000001&srNo=2&modifiedBy=EMP01
        [HttpDelete("DeleteMaterialRecord")]
        public async Task<IActionResult> DeleteMaterialRecord(string mcode, int srNo, string? modifiedBy)
        {
            var result = await _service.DeleteMaterialRecordAsync(mcode, srNo, modifiedBy);
            return Ok(new { success = result });
        }
    }
}