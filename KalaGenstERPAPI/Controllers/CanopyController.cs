using KalaGenset.ERP.Core.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CanopyController : ControllerBase
    {
        private readonly ICanopy _canopyService;

        public CanopyController(ICanopy canopyService)
        {
            _canopyService = canopyService;
        }

        [HttpGet("GetCanopyPlan/{strcompID}")]
        public async Task<IActionResult> GetCanopyPlan(string strcompID)
        {
            try
            {
                var result = await _canopyService.GetCanopyPlanAsync("Cpy_Plan", strcompID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, "An error occurred while fetching canopy plan details.");
            }
        }
    }
}

