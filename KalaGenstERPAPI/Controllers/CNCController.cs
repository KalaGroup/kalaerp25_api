using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.Request.Canopy;
using KalaGenset.ERP.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KalaGenset.ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CNCController : ControllerBase
    {

        private readonly ICNC _CNC;

        public CNCController(ICNC CNCServices)
        {
            _CNC = CNCServices;
        }

        [HttpGet("LoadMachine")]
        public async Task<IActionResult> LoadMachine(string pcCode)
        {
            try
            {
                var result = await _CNC.LoadMachineAsync(pcCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // TODO: log ex
                return StatusCode(500, "An error occurred while loading machines.");
            }
        }

        [HttpGet("LoadOSSupplier")]
        public async Task<IActionResult> LoadOSSupplier(string pcCode)
        {
            try
            {
                var result = await _CNC.LoadOSSupplierAsync(pcCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // TODO: log ex  →  _logger.LogError(ex, "LoadOSSupplier failed");
                return StatusCode(500, "An error occurred while loading OS suppliers.");
            }
        }


        [HttpGet("getCpyPrcddl")]
        public async Task<IActionResult> GetCpyPrcddl(string pcCode, string machineCode, string kva, string model, string planCode, string catId)
        {
            try
            {
                var result = await _CNC.GetCpyPrcddlAsync(pcCode, machineCode, kva, model, planCode, catId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "GetCpyPrcddl failed for {PCCode}", pcCode);
                return StatusCode(500, "An error occurred while loading dropdown data.");
            }
        }

        [HttpGet("LoadCatID")]
        public async Task<IActionResult> LoadCatID(string pcCode, string planCode)
        {
            try
            {
                var result = await _CNC.LoadCatIDAsync(pcCode, planCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "LoadCatID failed for {PCCode}", pcCode);
                return StatusCode(500, "An error occurred while loading category data.");
            }
        }

        [HttpGet("LoadProduct")]
        public async Task<IActionResult> LoadProduct(string pcCode)
        {
            try
            {
                var result = await _CNC.LoadProductAsync(pcCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "LoadProduct failed for {PCCode}", pcCode);
                return StatusCode(500, "An error occurred while loading products.");
            }
        }


        [HttpGet("getSheetPartDts")]
        public async Task<IActionResult> GetSheetPartDts( string pcCode, int sheetSrNo, string machineCode, string sheetPartcode,string planCode, string partcode, string catId)
        {
            try
            {
                var result = await _CNC.GetSheetPartDtsAsync(
                    pcCode, sheetSrNo, machineCode, sheetPartcode, planCode, partcode, catId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "GetSheetPartDts failed for {PCCode}", pcCode);
                return StatusCode(500, "An error occurred while loading sheet part details.");
            }
        }

        [HttpGet("GetTKitDts")]
        public async Task<IActionResult> GetTKitDts(string pcCode, string tKitId, int batchQty, string trnsType, string planCode, string prodCode)
        {
            try
            {
                var result = await _CNC.GetTKitDtsAsync(pcCode, tKitId, batchQty, trnsType, planCode, prodCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "GetTKitDts failed for {PCCode}", pcCode);
                return StatusCode(500, "An error occurred while loading turret kit details.");
            }
        }


        [HttpPost("CncSubmit")]
        public async Task<IActionResult> SubmitCNC([FromBody] CpyPrcCNCRequest req, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _CNC.SubmitCNCAsync(req, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "SubmitCNC failed for {PlanCode}", req?.PlanCode);
                return StatusCode(500, "An error occurred while submitting the CNC process.");
            }
        }

        [HttpGet("GetCNCCheckerCPPlanLoad")]
        public async Task<IActionResult> GetCNCCheckerCPPlanLoad([FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
            {
                return BadRequest("pcCode is required.");
            }

            var data = await _CNC.GetCheckerCPPlanLoadAsync(pcCode);
            return Ok(data);
        }

        // GET CNC/GetCNC_chekerDetails?compId=..&planCode=..&pcCode=..
        [HttpGet("GetCNC_chekerDetails")]
        public async Task<IActionResult> GetCNC_chekerDetails([FromQuery] string compId, [FromQuery] string planCode, [FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(compId) ||
                string.IsNullOrWhiteSpace(planCode) ||
                string.IsNullOrWhiteSpace(pcCode))
            {
                return BadRequest("compId, planCode and pcCode are all required.");
            }

            var data = await _CNC.GetCNC_chekerDetailsAsync(compId, planCode, pcCode);
            return Ok(data);
        }


        [HttpPost("CNC_chekerSave")]      // <-- matches the action segment
        public async Task<string> SubmitCNCChecker( [FromBody] CpyPrcCNCCheckerRequest CpyPrcCNCReq, CancellationToken ct)
        {
            return await _CNC.SubmitCncCheckerAsync(CpyPrcCNCReq, ct);
        }

    }
}
