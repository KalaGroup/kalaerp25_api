using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.Request.ControlPanel;
using KalaGenset.ERP.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ControlPanelJobCardController : ControllerBase
    {

        private readonly IControlPanel _controlPanelService;

        public ControlPanelJobCardController(IControlPanel controlPanelService)
        {
            _controlPanelService = controlPanelService;
        }


        [HttpGet("GetControlPanel/{lineWisePC}")]
        public async Task<IActionResult> GetControlPanel(string lineWisePC)
        {
            try
            {
                var result = await _controlPanelService.GetControlPanelAsync("ControlPanel", lineWisePC);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, "An error occurred while fetching canopy plan details.");
            }
        }

        [HttpPost]
        [Route("SubmitCP")]
        public async Task<string> SubmitControlPanel([FromBody] JobCard_CPRequest job_CPreq)
        {
            return await _controlPanelService.SubmitCPAsync(job_CPreq);
        }

        [HttpGet("GetCheckerCPLoad")]
        public async Task<IActionResult> GetCheckerCPLoad()
        {
            try
            {
                var result = await _controlPanelService.GetCheckerCPLoad();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, "An error occurred while fetching checker CP plan load.");
            }
        }


        [HttpGet("GetJobCardCpychecker/{compId}/{*planCode}")]
        public async Task<IActionResult> GetJobCardCpychecker(string compId, string planCode)
        {
            try
            {
                var result = await _controlPanelService.GetJobCardCpyCheckerAsync("CP_PlanCheker", compId, planCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, "An error occurred while fetching job card checker details.");
            }
        }

        [HttpPost]
        [Route("CPSaveChecker")]
        public async Task<string> CPCheckerSubmit([FromBody] CP_JobCardCheckerRequest job_CPCheckerreq)
        {
            return await _controlPanelService.CPCheckerSubmitAsync(job_CPCheckerreq);
        }
    }
}
