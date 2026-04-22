using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using KalaGenset.ERP.Core.RequestDTO;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using KalaGenset.ERP.Data.Models;
using Azure;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DGAssembllyController : ControllerBase
    {
        private readonly IEngineDGAssembly _engineDGAssembly;

        public DGAssembllyController(IEngineDGAssembly engineDGAssembly)
        {
            _engineDGAssembly = engineDGAssembly;
        }


        [HttpPost("GetStageScanDetails")]
        public async Task<IActionResult> GetStageScanDetails([FromBody] EngineDetailsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.SerialNo))
            {
                return BadRequest("Invalid request parameters.");
            }

            var result = await _engineDGAssembly.GetStageScanDetailsByQrSrNo(request);

            if (result == null)
            {
                return NotFound("No engine details found for the provided QR Serial No.");
            }

            return Ok(result);
        }

        [HttpGet("GetSelect6MData")]
        public async Task<IActionResult> GetSelect6MData()
        {
            var _6mdata = await _engineDGAssembly.Select6MFromDB().ToListAsync();

            return Ok(_6mdata);
        }

        [HttpGet("GetProcessCheckPoints/{stageName}/{statusName}")]
        public async Task<IActionResult> GetProcessCheckPoints(string stageName, string statusName)
        {
            var result = await _engineDGAssembly.FetchProcessCheckpointsFromDB(stageName, statusName);
            if (result == null || !result.Any())
            {
                return NotFound("No data found.");
            }
            return Ok(result);
        }

        [HttpGet("GetDGKitDetails/{PrdPartCode}/{PCCode_Old}/{PCCode_Act}")]
        public async Task<IActionResult> GetDGKitDetails(string PrdPartCode, string PCCode_Old, string PCCode_Act )
        {
            var result = await _engineDGAssembly.GetDGKitDetailsFromDB(PrdPartCode, PCCode_Old, PCCode_Act);
            if (result == null || !result.Any())
            {
                return NotFound("No data found.");
            }
            return Ok(result);
        }

        [HttpGet("GetTRKitDetails/{strPartcode}/{strDGSrNo}/{strPfbCode}")]
        public async Task<IActionResult> GetTRKitDetails(string strPartcode, string strDGSrNo, string strPfbCode)
        {
            var result = await _engineDGAssembly.GetTRKitDetailsFromDB(strPartcode, strDGSrNo, strPfbCode);
            if (result == null || !result.Any())
            {
                return NotFound("No data found.");
            }
            return Ok(result);
        }

        [HttpGet("GetMOFAddPartDts/{strMOFCode}/{AssemblyLine}")]
        public async Task<IActionResult> GetMOFAddPartDts(string strMOFCode, string AssemblyLine )
        {
            // Decode the encoded URL value (standard decoding)
            strMOFCode = Uri.UnescapeDataString(strMOFCode);

            var result = await _engineDGAssembly.GetMOFAdditionalPartDtsFromDB(strMOFCode, AssemblyLine);

            if (result == null || !result.Any())
            {
                return NotFound("No data found.");
            }

            return Ok(result);
        }


        [HttpPost("SubmitDGAssemblyDetails")]
        public async Task<IActionResult> SubmitDGAssemblyDetails([FromForm] DGAssemblySubmitRequest dgAssemblySubmitReq, [FromForm] string? PrcChkDtsJson)
        {
            bool stageStatus = await _engineDGAssembly.CheckStageStatus(dgAssemblySubmitReq.JBCode, dgAssemblySubmitReq.EngSrNo, dgAssemblySubmitReq.StageNo);
            if (stageStatus)
            {
                return BadRequest($"This Stage For Jobcard:- {dgAssemblySubmitReq.JBCode} With Eng Serial No:- {dgAssemblySubmitReq.EngSrNo} Is Already Completed!");
            }
            if (PrcChkDtsJson != null)
            {
                dgAssemblySubmitReq.PrcChkDts = JsonConvert.DeserializeObject<List<ProcessCheckpointDTO>>(PrcChkDtsJson);
                await _engineDGAssembly.SubmitDGAssemblyDetails(dgAssemblySubmitReq);
                return Ok(new { Message = "Stage Started Successfully..!" });
            }
            else
            {
                await _engineDGAssembly.SubmitDGAssemblyDetails(dgAssemblySubmitReq);
                return Ok(new { Message = "Stage Started Successfully..!" });
            }
        }

        [HttpPost("SubmitDGStage4Details")]
        public async Task<IActionResult> SubmitDGStage4Details([FromForm] DGAssemblySubmitRequest dgAssemblySubmitReq, [FromForm] string? PrcChkDtsJson, [FromForm] string? DGKitDetailJson)
        {
            bool stageStatus = await _engineDGAssembly.CheckStageStatus(dgAssemblySubmitReq.JobCardCode, dgAssemblySubmitReq.EngSrNo, dgAssemblySubmitReq.StageNo);
            if (stageStatus)
            {
                return BadRequest($"This Stage For Jobcard:- {dgAssemblySubmitReq.JobCardCode} With Eng Serial No:- {dgAssemblySubmitReq.EngSrNo} Is Already Completed!");
            }

            if (!string.IsNullOrEmpty(PrcChkDtsJson))
                dgAssemblySubmitReq.PrcChkDts = JsonConvert.DeserializeObject<List<ProcessCheckpointDTO>>(PrcChkDtsJson);

            if (!string.IsNullOrEmpty(DGKitDetailJson))
                dgAssemblySubmitReq.DGKitDetails = JsonConvert.DeserializeObject<List<DgKitDTO>>(DGKitDetailJson);

            var result = await _engineDGAssembly.SubmitDGAssemblyStage4Details(dgAssemblySubmitReq);
            return Ok(new { Message = result });

        }

        [HttpPost("GetTestReportDetails")]
        public async Task<IActionResult> GetTestReportDetails([FromBody] TestReportDetailsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.strDGSrNo))
            {
                return BadRequest("Invalid request parameters.");
            }

            var result = await _engineDGAssembly.GetTestReportScanDetails(request);

            if (result == null)
            {
                return NotFound("No DG details found for the provided QR Serial No.");
            }

            return Ok(result);
        }
        [HttpGet("GetJobCardDGDetails/{strJobCardType}/{strcompID}")]
        public async Task<IActionResult> GetJobCardDGDetails(string strJobCardType, string strcompID, string assemblyLine)
        {
            if (strcompID == null || string.IsNullOrEmpty(strcompID))
            {
                return BadRequest("Invalid request parameters.");
            }

            var result = await _engineDGAssembly.GetJobCardDGDtsAsync(strJobCardType, strcompID, assemblyLine);

            // var firstItem = result?.FirstOrDefault();  // Safely get the first item

            if (result == null)
            {
                return NotFound("No JobCard Details found for the provided Input.");
            }
            return Ok(result);  // Returns just the object, not inside an array
        }

        [HttpPost("GetJobCardReportDetails")]
        public async Task<IActionResult> GetJobCardReportDetails([FromBody] JobCardRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Type))
            {
                return BadRequest("Invalid request parameters.");
            }

            var result = await _engineDGAssembly.GetJobCard1RptAsync(request);

            // var firstItem = result?.FirstOrDefault();  // Safely get the first item

            if (result == null)
            {
                return NotFound("No JobCard Details found for the provided Input.");
            }
            return Ok(result);  // Returns just the object, not inside an array
        }

        [HttpPost("GetJobCard2ReportDetails")]
        public async Task<IActionResult> GetJobCard2ReportDetails([FromBody] JobCardRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Type))
            {
                return BadRequest("Invalid request parameters.");
            }

            var result = await _engineDGAssembly.GetJobCard2RptAsync(request);

            // var firstItem = result?.FirstOrDefault();  // Safely get the first item

            if (result == null)
            {
                return NotFound("No JobCard Details found for the provided Input.");
            }
            return Ok(result);  // Returns just the object, not inside an array
        }

        [HttpPost("SubmitTestReportDetails")]
        public async Task<IActionResult> SubmitPackingSlipDetails([FromForm] TestReportSubmitDetails testReportSubmitReq, [FromForm] string? TRPrcChkDtsJson, [FromForm] string? TRDGKitDetailJson)
        {
            if (!string.IsNullOrEmpty(TRPrcChkDtsJson))
                testReportSubmitReq.TRPrcChkDts = JsonConvert.DeserializeObject<List<TRProcessCheckpointDTO>>(TRPrcChkDtsJson);

            if (!string.IsNullOrEmpty(TRDGKitDetailJson))
                testReportSubmitReq.TRDGKitDetails = JsonConvert.DeserializeObject<List<TRDgKitDTO>>(TRDGKitDetailJson);

            var result = await _engineDGAssembly.SubmitTestReportDetails(testReportSubmitReq);
            return Ok(new { Message = result });

        }

        [HttpPost("GetPackingSlipDetails")]
        public async Task<IActionResult> GetPackingSlipDetails([FromBody] PackingSlipDetailsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.StrDGSrNo))
            {
                return BadRequest("Invalid request parameters.");
            }

            var result = await _engineDGAssembly.GetPackingSlipScanDtsAsync(request);
            var firstItem = result?.FirstOrDefault();  // Safely get the first item

            if (firstItem == null)
            {
                return NotFound("No Packing Slip found for the provided QR Serial No.");
            }
            return Ok(firstItem);  // Returns just the object, not inside an array
        }

        [HttpPost("SubmitPackingSlipDetails")]
        public async Task<IActionResult> SubmitPackingSlipDetails([FromForm] PackingSlipSubmitDetails packingSlipSubmitReq, [FromForm] string? psMOFAddPartDetailsJson)
        {
            if (!string.IsNullOrEmpty(psMOFAddPartDetailsJson))
                packingSlipSubmitReq.MOFAddParts = JsonConvert.DeserializeObject<List<MOFAddPartDetailsDTO>>(psMOFAddPartDetailsJson);

            var result = await _engineDGAssembly.SubmitPackingSlipDetails(packingSlipSubmitReq);
            return Ok(new { Message = result });
        }

        [HttpPost("SaveJobCard2Details")]
        public async Task<IActionResult> SaveJobCard2Details([FromBody] Jobcard2SubmitDetails request)
        {
            if (request == null || request.JobCard2Dts == null || request.JobCard2Dts.Count == 0)
            {
                return BadRequest("Invalid data received");
            }

            string jobCardNo = await _engineDGAssembly.SubmitJobCard2Details(request);
            if (jobCardNo.StartsWith("Insufficient Stock For Part") ||
                jobCardNo.StartsWith("Panel Not Selected For DG") ||
                jobCardNo.StartsWith("JobCard1(Without Panel) Not available For DG") ||
                jobCardNo.StartsWith("Engine SrNo Not available For DG") ||
                jobCardNo.StartsWith("Alternator SrNo Not available For DG") ||
                jobCardNo.StartsWith("Battery SrNo Not available For DG") ||
                jobCardNo.StartsWith("Canopy SrNo Not available For DG") ||
                jobCardNo.StartsWith("CP SrNo Not available For DG"))
            {
                return Ok(new { Message = jobCardNo });
            }
            else
            {
                return Ok(new { Message = $"Jobcard Details Saved Successfully And JobcardNo is - {jobCardNo}" });
            }
        }

        //[HttpPost("UploadTestReportAndPDIRVideo")]
        //[RequestSizeLimit(104857600)] // 100MB
        //[RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100MB
        //public async Task<IActionResult> UploadTestReportAndPDIRVideo([FromForm] UploadVideopdfDGAssemblyRequest uploadVideopdfDGAssemblyReq)
        //{
        //    var result = await _engineDGAssembly.UploadVideoAndPDFAsync(uploadVideopdfDGAssemblyReq);
        //    return Ok(result);
        //}
        [HttpPost("UploadTestReportAndPDIRVideo")]
        [RequestSizeLimit(524288000)] // 500MB
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // 500MB
        public async Task<IActionResult> UploadTestReportAndPDIRVideo([FromForm] UploadVideopdfDGAssemblyRequest uploadVideopdfDGAssemblyReq)
        {
            var result = await _engineDGAssembly.UploadVideoAndPDFAsync(uploadVideopdfDGAssemblyReq);
            return Ok(result);
        }
    }
}