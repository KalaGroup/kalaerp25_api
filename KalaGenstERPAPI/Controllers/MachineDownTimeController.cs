using System;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachineDownTimeController : ControllerBase
    {
        private readonly IMachineDownTime _service;

        public MachineDownTimeController(IMachineDownTime service)
        {
            _service = service;
        }

        // GET api/MachineDownTime/GetDepartments?companyCode=01
        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments(string companyCode)
        {
            var result = await _service.GetDepartmentsAsync(companyCode);
            return Ok(result);
        }

        // GET api/MachineDownTime/GetMachines?departmentCode=01.106
        [HttpGet("GetMachines")]
        public async Task<IActionResult> GetMachines(string departmentCode)
        {
            var result = await _service.GetMachinesByDepartmentAsync(departmentCode);
            return Ok(result);
        }

        // GET api/MachineDownTime/GetDownTimeRecords?companyCode=01&date=2026-06-23&departmentCode=01.106
        [HttpGet("GetDownTimeRecords")]
        public async Task<IActionResult> GetDownTimeRecords(string companyCode, DateTime? date, string? departmentCode)
        {
            var result = await _service.GetDownTimeRecordsAsync(companyCode, date, departmentCode);
            return Ok(result);
        }

        // GET api/MachineDownTime/GetDownTimeTrend?companyCode=01&fromDate=2026-06-01&toDate=2026-06-30
        [HttpGet("GetDownTimeTrend")]
        public async Task<IActionResult> GetDownTimeTrend(string companyCode, DateTime fromDate, DateTime toDate)
        {
            var result = await _service.GetDownTimeTrendAsync(companyCode, fromDate, toDate);
            return Ok(result);
        }

        // POST api/MachineDownTime/SaveDownTimeBatch
        [HttpPost("SaveDownTimeBatch")]
        public async Task<IActionResult> SaveDownTimeBatch([FromBody] MachineDownTimeBatchRequest request)
        {
            var result = await _service.SaveDownTimeBatchAsync(request);
            return Ok(new { success = result });
        }

        // DELETE api/MachineDownTime/DeleteDownTimeRecord?mcode=MDWN/26-27/01000001&srNo=2&modifiedBy=EMP01
        [HttpDelete("DeleteDownTimeRecord")]
        public async Task<IActionResult> DeleteDownTimeRecord(string mcode, int srNo, string? modifiedBy)
        {
            var result = await _service.DeleteDownTimeRecordAsync(mcode, srNo, modifiedBy);
            return Ok(new { success = result });
        }
    }
}