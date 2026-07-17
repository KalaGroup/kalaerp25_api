using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using static KalaGenset.ERP.Core.Services.CanopyService;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CanopyController : ControllerBase
    {
        private readonly ICanopy _dc;

        public CanopyController(ICanopy canopyService)
        {
            _dc = canopyService;
        }

        //Sheet metal job card process Maker 

        [HttpGet("GetCanopyPlan/{lineWisePC}")]
        public async Task<IActionResult> GetCanopyPlan(string lineWisePC)
        {
            try
            {
                var result = await _dc.GetCanopyPlanAsync("Cpy_Plan", lineWisePC);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, "An error occurred while fetching canopy plan details.");
            }
        }

        [HttpPost]
        [Route("JobCard_Cpy/Submit")]
        public async Task<string> SubmitCanopy([FromBody] JobCard_CpyRequest job_Cpyreq)
        {
            return await _dc.SubmitAsync(job_Cpyreq);
        }

        ///  Sheet metal job card Checker

        [HttpGet("GetJobCardCpychecker/{compId}/{*planCode}")]
        public async Task<IActionResult> GetJobCardCpychecker(string compId, string planCode)
        {
            try
            {
                var result = await _dc.GetJobCardCpyCheckerAsync("Cpy_PlanCheker", compId, planCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, "An error occurred while fetching job card checker details.");
            }
        }

        [HttpGet("GetJobCardCpyCheckerDone")]
        public async Task<IActionResult> GetJobCardCpyCheckerDone()
        {
            try
            {
                var result = await _dc.GetJobCardCpyCheckerDoneAsync("Cpy_PlanChekerDone", "0", "0");
                return Ok(result);
            }
            catch (Exception ex)
            {
                // TODO: log ex
                return StatusCode(500, "An error occurred while fetching job card checker details.");
            }
        }


        [HttpGet("JobCard_Cpy/6MTypes")]
        public async Task<IActionResult> Get6MTypes()
        {
            try
            {
                var result = await _dc.Get6MTypesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [HttpGet("JobCard_Cpy/CorReqEmpName")]
        public async Task<IActionResult> GetJobcardCorReqEmpName()
        {
            try
            {
                var result = await _dc.JobcardCorReqEmpNameAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("JobCard_Cpy/SaveChecker")]
        public async Task<string> CheckerSubmit([FromBody] Canopy_JobCardCheckerRequest job_CpyCheckerreq)
        {
            return await _dc.CheckerSubmitAsync(job_CpyCheckerreq);
        }


        [HttpGet("GetCheckerCPPlanLoad")]
        public async Task<IActionResult> GetCheckerCPPlanLoad()
        {
            try
            {
                var result = await _dc.GetCheckerCPPlanLoadAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, "An error occurred while fetching checker CP plan load.");
            }
        }

        [HttpGet("GetStageSheetData")]
        public async Task<IActionResult> GetStageSheetData([FromQuery] string cpCode, [FromQuery] string partCode, [FromQuery] string stage, [FromQuery] string pcCode)
        {
            try
            {
                var result = await _dc.GetStageSheetDataAsync(cpCode, partCode, stage, pcCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching stage sheet data.");
            }
        }

        [HttpGet("GetLineByProcess/{ProcessName}")]
        public async Task<IActionResult> GetLineByProcess(string ProcessName,[FromQuery] string compCode = "")
        {
            try
            {
                var result = await _dc.GetLineByProcessAsync(ProcessName, compCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching canopy plan details.");
            }
        }


        ///Sheet metal job card hold process 
  
        // GET api/Canopy/GetconopyHold?compCode=01
        [HttpGet("GetconopyHold")]
        public async Task<ActionResult> GetconopyHold([FromQuery] string compCode)
        {
            if (string.IsNullOrWhiteSpace(compCode))
                return BadRequest("compCode is required.");

            var data = await _dc.GetConopyHoldAsync(compCode.Trim());
            return Ok(data);
        }


        [HttpPost]
        [Route("JobCardConopyReqInActiveHold")]
        public async Task<string> JobCardConopyReqInActiveHold(
                    [FromBody] Canopy_JobCardHoldRequest job_CpyHoldreq)
        {
            return await _dc.JobCardConopyReqInActiveHoldAsync(job_CpyHoldreq);
        }

    }
}



