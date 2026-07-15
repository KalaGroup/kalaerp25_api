using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // pcCode = selected line's LineWisePC — controls the KVA band shown.
        [HttpGet("GetFlatPackCanopyOptions")]
        public async Task<IActionResult> GetFlatPackCanopyOptions([FromQuery] string? pcCode)
        {
            var rows = await _canopyAssemblyService.GetFlatPackCanopyOptionsAsync(pcCode ?? string.Empty);
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

        // ── Canopy Assembly Process (operator-side) ───────────────────
        [HttpGet("GetCanopyProcessMachineList")]
        public async Task<IActionResult> GetCanopyProcessMachineList([FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyProcessMachineListAsync(pcCode.Trim());
            return Ok(rows ?? new List<CanopyProcessMachineDto>());
        }

        [HttpGet("GetCanopyProcessKvaList")]
        public async Task<IActionResult> GetCanopyProcessKvaList(
            [FromQuery] string machineCode,
            [FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(machineCode))
                return BadRequest("machineCode is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyProcessKvaListAsync(
                machineCode.Trim(), pcCode.Trim());
            return Ok(rows ?? new List<CanopyProcessKvaDto>());
        }

        [HttpGet("GetCanopyProcessModelList")]
        public async Task<IActionResult> GetCanopyProcessModelList(
            [FromQuery] string machineCode,
            [FromQuery] string kva,
            [FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(machineCode))
                return BadRequest("machineCode is required.");
            if (string.IsNullOrWhiteSpace(kva))
                return BadRequest("kva is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyProcessModelListAsync(
                machineCode.Trim(), kva.Trim(), pcCode.Trim());
            return Ok(rows ?? new List<CanopyProcessModelDto>());
        }

        [HttpGet("GetCanopyProcessPlanContext")]
        public async Task<IActionResult> GetCanopyProcessPlanContext(
            [FromQuery] string machineCode,
            [FromQuery] string kva,
            [FromQuery] string model,
            [FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(machineCode))
                return BadRequest("machineCode is required.");
            if (string.IsNullOrWhiteSpace(kva))
                return BadRequest("kva is required.");
            if (string.IsNullOrWhiteSpace(model))
                return BadRequest("model is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var ctx = await _canopyAssemblyService.GetCanopyProcessPlanContextAsync(
                machineCode.Trim(), kva.Trim(), model.Trim(), pcCode.Trim());
            return Ok(ctx);
        }

        [HttpGet("GetCanopyProcessKitList")]
        public async Task<IActionResult> GetCanopyProcessKitList(
            [FromQuery] string machineCode,
            [FromQuery] string pcCode,
            [FromQuery] string planCode,
            [FromQuery] string partCode)
        {
            if (string.IsNullOrWhiteSpace(machineCode))
                return BadRequest("machineCode is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyProcessKitListAsync(
                machineCode.Trim(), pcCode.Trim(), planCode?.Trim() ?? string.Empty,
                partCode?.Trim() ?? string.Empty);
            return Ok(rows ?? new List<CanopyProcessKitDto>());
        }

        [HttpGet("GetCanopyProcessKitContext")]
        public async Task<IActionResult> GetCanopyProcessKitContext(
            [FromQuery] string machineCode,
            [FromQuery] string kitCode,
            [FromQuery] string pcCode,
            [FromQuery] string planCode,
            [FromQuery] string partCode)
        {
            if (string.IsNullOrWhiteSpace(machineCode))
                return BadRequest("machineCode is required.");
            if (string.IsNullOrWhiteSpace(kitCode))
                return BadRequest("kitCode is required.");
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var ctx = await _canopyAssemblyService.GetCanopyProcessKitContextAsync(
                machineCode.Trim(), kitCode.Trim(), pcCode.Trim(),
                planCode?.Trim() ?? string.Empty, partCode?.Trim() ?? string.Empty);
            return Ok(ctx);
        }

        [HttpGet("GetCanopyProcessPartRows")]
        public async Task<IActionResult> GetCanopyProcessPartRows(
            [FromQuery] string pcCode,
            [FromQuery] int prcQty,
            [FromQuery] string cpyPartCode,
            [FromQuery] string planCode,
            [FromQuery] string bomCode,
            [FromQuery] string pfbCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            if (prcQty <= 0)
                return BadRequest("prcQty must be greater than 0.");
            if (string.IsNullOrWhiteSpace(bomCode))
                return BadRequest("bomCode is required.");
            if (string.IsNullOrWhiteSpace(pfbCode))
                return BadRequest("pfbCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyProcessPartRowsAsync(
                pcCode.Trim(), prcQty, cpyPartCode?.Trim() ?? string.Empty,
                planCode?.Trim() ?? string.Empty, bomCode.Trim(), pfbCode.Trim());
            return Ok(rows ?? new List<CanopyProcessPartRowDto>());
        }

        [HttpGet("GetCanopyProcessAssemblyKitRows")]
        public async Task<IActionResult> GetCanopyProcessAssemblyKitRows(
            [FromQuery] string pcCode,
            [FromQuery] int prcQty,
            [FromQuery] string cpyPartCode,
            [FromQuery] string planCode,
            [FromQuery] string bomCode,
            [FromQuery] string pfbCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            if (prcQty <= 0)
                return BadRequest("prcQty must be greater than 0.");
            if (string.IsNullOrWhiteSpace(bomCode))
                return BadRequest("bomCode is required.");
            if (string.IsNullOrWhiteSpace(pfbCode))
                return BadRequest("pfbCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyProcessAssemblyKitRowsAsync(
                pcCode.Trim(), prcQty, cpyPartCode?.Trim() ?? string.Empty,
                planCode?.Trim() ?? string.Empty, bomCode.Trim(), pfbCode.Trim());
            return Ok(rows ?? new List<CanopyProcessAssemblyKitRowDto>());
        }

        [HttpPost("SubmitCanopyProcess")]
        public async Task<IActionResult> SubmitCanopyProcess(
            [FromBody] SubmitCanopyProcessRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");

            try
            {
                var resp = await _canopyAssemblyService.SubmitCanopyProcessAsync(req);
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

        // ── Canopy Assembly Process Checker (quality review side) ────
        [HttpGet("GetCanopyProcessCheckPendingList")]
        public async Task<IActionResult> GetCanopyProcessCheckPendingList([FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyProcessCheckPendingListAsync(pcCode.Trim());
            return Ok(rows ?? new List<CanopyProcessCheckPendingRowDto>());
        }

        [HttpGet("GetCanopyProcessCheckContext")]
        public async Task<IActionResult> GetCanopyProcessCheckContext([FromQuery] string pfbCode)
        {
            if (string.IsNullOrWhiteSpace(pfbCode))
                return BadRequest("pfbCode is required.");
            var ctx = await _canopyAssemblyService.GetCanopyProcessCheckContextAsync(pfbCode.Trim());
            return Ok(ctx);
        }

        [HttpGet("GetCanopyProcessCheckReport")]
        public async Task<IActionResult> GetCanopyProcessCheckReport(
            [FromQuery] string pcCode,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            if (fromDate == default || toDate == default)
                return BadRequest("fromDate and toDate are required.");

            var rows = await _canopyAssemblyService.GetCanopyProcessCheckReportAsync(
                pcCode.Trim(), fromDate, toDate);
            return Ok(rows ?? new List<CanopyProcessCheckReportRowDto>());
        }

        [HttpPost("SaveCanopyProcessCheck")]
        public async Task<IActionResult> SaveCanopyProcessCheck(
            [FromBody] SaveCanopyProcessCheckRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");
            try
            {
                var resp = await _canopyAssemblyService.SaveCanopyProcessCheckAsync(req);
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

        // ── Canopy Plan Checker (plan-authorization side) ───────────
        [HttpGet("GetCanopyPlanCheckPendingList")]
        public async Task<IActionResult> GetCanopyPlanCheckPendingList([FromQuery] string pcCode)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            var rows = await _canopyAssemblyService.GetCanopyPlanCheckPendingListAsync(pcCode.Trim());
            return Ok(rows ?? new List<CanopyPlanCheckPendingRowDto>());
        }

        [HttpGet("GetCanopyPlanCheckContext")]
        public async Task<IActionResult> GetCanopyPlanCheckContext([FromQuery] string cpCode)
        {
            if (string.IsNullOrWhiteSpace(cpCode))
                return BadRequest("cpCode is required.");
            var ctx = await _canopyAssemblyService.GetCanopyPlanCheckContextAsync(cpCode.Trim());
            return Ok(ctx);
        }

        [HttpPost("SaveCanopyPlanCheck")]
        public async Task<IActionResult> SaveCanopyPlanCheck(
            [FromBody] SaveCanopyPlanCheckRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");
            try
            {
                var resp = await _canopyAssemblyService.SaveCanopyPlanCheckAsync(req);
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

        [HttpGet("GetCanopyPlanCheckReport")]
        public async Task<IActionResult> GetCanopyPlanCheckReport(
            [FromQuery] string pcCode,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (string.IsNullOrWhiteSpace(pcCode))
                return BadRequest("pcCode is required.");
            if (fromDate == default || toDate == default)
                return BadRequest("fromDate and toDate are required.");
            var rows = await _canopyAssemblyService.GetCanopyPlanCheckReportAsync(
                pcCode.Trim(), fromDate, toDate);
            return Ok(rows ?? new List<CanopyPlanCheckReportRowDto>());
        }

        // Multipart upload — matches legacy CpyPrc/UploadFiles: multipart form with
        // fileUpload (or file field), FrmEcode, FileUploadType=Save|Delete.
        // Files land in C:\TempERPFile\TempPrcCpy\{FrmEcode}\ and are moved to
        // the permanent archive at Submit time.
        [HttpPost("UploadCanopyProcessFile")]
        [DisableRequestSizeLimit]
        public IActionResult UploadCanopyProcessFile()
        {
            const string tempBase = @"C:\TempERPFile\TempPrcCpy";

            var frmEcode = Request.Form["FrmEcode"].ToString().Trim();
            var uploadType = Request.Form["FileUploadType"].ToString().Trim();
            if (string.IsNullOrEmpty(frmEcode)) return BadRequest("FrmEcode is required.");

            var empPath = Path.Combine(tempBase, frmEcode);
            Directory.CreateDirectory(empPath);

            if (string.Equals(uploadType, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                var target = Request.Form["fileUpload"].ToString().Trim();
                if (string.IsNullOrEmpty(target)) return BadRequest("fileUpload (filename) is required for Delete.");
                var filePath = Path.Combine(empPath, target);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
                return Ok(new { message = "File Deleted" });
            }

            var files = Request.Form.Files;
            if (files == null || files.Count == 0) return BadRequest("No files provided.");
            int uploaded = 0;
            foreach (var file in files)
            {
                if (file.Length <= 0) continue;
                var name = Path.GetFileName(file.FileName);
                var dst = Path.Combine(empPath, name);
                if (System.IO.File.Exists(dst)) continue;   // skip duplicates
                using var fs = new FileStream(dst, FileMode.Create, FileAccess.Write);
                file.CopyTo(fs);
                uploaded++;
            }
            return uploaded > 0
                ? Ok(new { message = $"{uploaded} File(s) Uploaded Successfully" })
                : BadRequest(new { message = "Upload Failed" });
        }
    }
}
