using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.CanopyAssembly;
using KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CanopyAssemblyController : ControllerBase
    {
        private readonly ICanopyAssembly _canopyAssemblyService;

        public CanopyAssemblyController(ICanopyAssembly canopyAssemblyService)
        {
            _canopyAssemblyService = canopyAssemblyService;
        }

        // ── Flat Pack Canopy Plan Report ──────────────────────────────
        [HttpGet("GetFlatPackCanopyPlanReport")]
        public async Task<IActionResult> GetFlatPackCanopyPlanReport(
            [FromQuery] string pcCode,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            if (fromDate == default || toDate == default)
                return BadRequest("fromDate and toDate are required.");

            var result = await _canopyAssemblyService
                .GetFlatPackCanopyPlanReportAsync(pcCode.Trim(), fromDate, toDate);

            return Ok(result ?? new List<Dictionary<string, object?>>());
        }

        // ── Flat Pack Canopy Assembly Process ─────────────────────────
        [HttpGet("GetFlatPackCanopyOptions")]
        public async Task<IActionResult> GetFlatPackCanopyOptions()
        {
            var rows = await _canopyAssemblyService.GetFlatPackCanopyOptionsAsync();
            return Ok(rows ?? new List<FlatPackCanopyOptionDto>());
        }

        [HttpGet("GetFlatPackBindPrimary")]
        public async Task<IActionResult> GetFlatPackBindPrimary(
            [FromQuery] string canopyPartCode,
            [FromQuery] string processType,
            [FromQuery] string? heading)
        {
            if (string.IsNullOrWhiteSpace(canopyPartCode))
                return BadRequest("canopyPartCode is required.");
            if (string.IsNullOrWhiteSpace(processType))
                return BadRequest("processType is required.");

            var resp = await _canopyAssemblyService.GetFlatPackBindPrimaryAsync(
                canopyPartCode.Trim(),
                processType.Trim(),
                heading);
            return Ok(resp);
        }

        [HttpPost("GetFlatPackProcessDetails")]
        public async Task<IActionResult> GetFlatPackProcessDetails(
            [FromBody] FlatPackProcessDetailsRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");
            if (string.IsNullOrWhiteSpace(req.PCCode)) return BadRequest("PCCode is required.");
            if (string.IsNullOrWhiteSpace(req.PartCode)) return BadRequest("PartCode is required.");
            if (string.IsNullOrWhiteSpace(req.ProcessType)) return BadRequest("ProcessType is required.");
            if (req.ProcessQty <= 0) return BadRequest("ProcessQty must be greater than 0.");

            var resp = await _canopyAssemblyService.GetFlatPackProcessDetailsAsync(req);
            return Ok(resp);
        }

        [HttpPost("SubmitFlatPackProcess")]
        public async Task<IActionResult> SubmitFlatPackProcess(
            [FromBody] FlatPackSubmitRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");

            try
            {
                var resp = await _canopyAssemblyService.SubmitFlatPackProcessAsync(req);
                return Ok(resp);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── Canopy Assembly Plan ───────────────────────────────────────
        [HttpGet("GetCanopyPlanPartOptions")]
        public async Task<IActionResult> GetCanopyPlanPartOptions(
            [FromQuery] string? searchText,
            [FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");

            var rows = await _canopyAssemblyService
                .GetCanopyPlanPartOptionsAsync(searchText, pcCode.Trim());
            return Ok(rows ?? new List<CanopyPlanPartOptionDto>());
        }

        [HttpGet("GetCanopyPlanPartContext")]
        public async Task<IActionResult> GetCanopyPlanPartContext(
            [FromQuery] string partCode,
            [FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(partCode))
                return BadRequest("partCode is required.");

            var ctx = await _canopyAssemblyService
                .GetCanopyPlanPartContextAsync(partCode.Trim(), pcCode?.Trim() ?? string.Empty);
            return Ok(ctx);
        }

        [HttpGet("GetCanopyPlanCheckerMakerRows")]
        public async Task<IActionResult> GetCanopyPlanCheckerMakerRows(
            [FromQuery] string lineWisePC)
        {
            if (string.IsNullOrWhiteSpace(lineWisePC))
                return BadRequest("lineWisePC is required.");

            var rows = await _canopyAssemblyService
                .GetCanopyPlanCheckerMakerRowsAsync(lineWisePC.Trim());
            return Ok(rows ?? new List<CanopyPlanCheckerMakerRowDto>());
        }

        [HttpPost("SubmitCanopyPlan")]
        public async Task<IActionResult> SubmitCanopyPlan(
            [FromBody] SubmitCanopyPlanRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");

            try
            {
                var resp = await _canopyAssemblyService.SubmitCanopyPlanAsync(req);
                return Ok(resp);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
