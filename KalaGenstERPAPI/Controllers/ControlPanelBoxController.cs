using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.ControlPanelBox;
using KalaGenset.ERP.Core.ResponseDTO.ControlPanelBox;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ControlPanelBoxController : ControllerBase
    {
        private readonly IControlPanelBox _controlPanelBox;

        public ControlPanelBoxController(IControlPanelBox controlPanelBox)
        {
            _controlPanelBox = controlPanelBox;
        }

        // GET api/ControlPanelBox/GetPlanRowsByKva?kva=250
        // Returns candidate Control Panel Box BOMs for the picked KVA
        // (TOP 25, one per unique KitCode, ordered by PartDesc).
        [HttpGet("GetPlanRowsByKva")]
        public async Task<IActionResult> GetPlanRowsByKva([FromQuery] string kva)
        {
            if (string.IsNullOrWhiteSpace(kva))
                return BadRequest("kva is required.");

            var rows = await _controlPanelBox.GetPlanRowsByKvaAsync(kva.Trim());
            // Empty array instead of 404 so the UI grid renders "no records" cleanly.
            return Ok(rows ?? new List<ControlPanelBoxPlanRowDto>());
        }

        // POST api/ControlPanelBox/SubmitPlan
        // Header-only insert into CanopyPlan via InsertCanopyPlan_Maker_Checker.
        // PCCode / CompanyCode / PCCode_Act / Checker1 are hardcoded server-side.
        [HttpPost("SubmitPlan")]
        public async Task<IActionResult> SubmitPlan([FromBody] SubmitControlPanelBoxPlanRequest request)
        {
            if (request == null)             return BadRequest("Request body is required.");
            if (request.FromDt == default)   return BadRequest("FromDt is required.");
            if (request.ToDt   == default)   return BadRequest("ToDt is required.");
            if (request.FromDt > request.ToDt) return BadRequest("FromDt cannot be after ToDt.");

            try
            {
                var response = await _controlPanelBox.SubmitPlanAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ControlPanelBox.SubmitPlan] {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Failed to save plan: {ex.Message}");
            }
        }
    }
}
