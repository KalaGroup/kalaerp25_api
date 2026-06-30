using System;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManpowerStatusController : ControllerBase
    {
        private readonly IManpowerStatus _service;

        public ManpowerStatusController(IManpowerStatus service)
        {
            _service = service;
        }

        // GET api/ManpowerStatus/GetDepartments?companyCode=01
        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments(string companyCode)
        {
            var result = await _service.GetDepartmentsAsync(companyCode);
            return Ok(result);
        }

        // GET api/ManpowerStatus/GetStations?pcId=943&companyCode=01
        [HttpGet("GetStations")]
        public async Task<IActionResult> GetStations(int pcId, string companyCode)
        {
            var result = await _service.GetStationsByDepartmentAsync(pcId, companyCode);
            return Ok(result);
        }

        // GET api/ManpowerStatus/GetManpowerRecords?companyCode=01&date=2026-06-19&shift=F&pcId=943
        [HttpGet("GetManpowerRecords")]
        public async Task<IActionResult> GetManpowerRecords(string companyCode, DateTime? date, string? shift, int? pcId)
        {
            var result = await _service.GetManpowerRecordsAsync(companyCode, date, shift, pcId);
            return Ok(result);
        }

        // GET api/ManpowerStatus/GetShortageTrend?companyCode=01&fromDate=2026-06-01&toDate=2026-06-30
        [HttpGet("GetShortageTrend")]
        public async Task<IActionResult> GetShortageTrend(string companyCode, DateTime fromDate, DateTime toDate)
        {
            var result = await _service.GetShortageTrendAsync(companyCode, fromDate, toDate);
            return Ok(result);
        }

        // POST api/ManpowerStatus/SaveManpowerBatch
        [HttpPost("SaveManpowerBatch")]
        public async Task<IActionResult> SaveManpowerBatch([FromBody] ManpowerStatusBatchRequest request)
        {
            var result = await _service.SaveManpowerBatchAsync(request);
            return Ok(new { success = result });
        }

        // DELETE api/ManpowerStatus/DeleteManpowerRecord?mcode=MANP/26-27/01000001&srNo=2&modifiedBy=EMP01
        [HttpDelete("DeleteManpowerRecord")]
        public async Task<IActionResult> DeleteManpowerRecord(string mcode, int srNo, string? modifiedBy)
        {
            var result = await _service.DeleteManpowerRecordAsync(mcode, srNo, modifiedBy);
            return Ok(new { success = result });
        }
    }
}