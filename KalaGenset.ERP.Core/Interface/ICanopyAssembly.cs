using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KalaGenset.ERP.Core.Request.CanopyAssembly;
using KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly;

namespace KalaGenset.ERP.Core.Interface
{
    public interface ICanopyAssembly
    {
        // ── Flat Pack Canopy Plan Report ────────────────────────────
        Task<List<Dictionary<string, object?>>> GetFlatPackCanopyPlanReportAsync(
            string pcCode,
            DateTime fromDate,
            DateTime toDate);

        // ── Flat Pack Canopy Assembly Process ───────────────────────
        // Canopy dropdown (PartToCDetailsSupplier ∪ MTODts)
        Task<List<FlatPackCanopyOptionDto>> GetFlatPackCanopyOptionsAsync();

        // Derive the "Part Desc" textbox value once Canopy + ProcessType picked
        Task<FlatPackBindPrimaryResponse> GetFlatPackBindPrimaryAsync(
            string canopyPartCode,
            string processType,
            string? heading);

        // Search → grid + master rate/wt/sqft/CR/HR
        Task<FlatPackProcessDetailsResponse> GetFlatPackProcessDetailsAsync(
            FlatPackProcessDetailsRequest req);

        // Save → master + details + WIP + serials + (optional) Kanban
        Task<FlatPackSubmitResponse> SubmitFlatPackProcessAsync(
            FlatPackSubmitRequest req);

        // ── Canopy Assembly Plan (manual planning) ──────────────────
        // Lazy-loaded canopy part dropdown (BOMDetails kit family '40%')
        Task<List<CanopyPlanPartOptionDto>> GetCanopyPlanPartOptionsAsync(
            string? searchText,
            string pcCode);

        // After a part is picked — derives BomCode + stock + pending qty
        Task<CanopyPlanPartContextDto> GetCanopyPlanPartContextAsync(
            string partCode,
            string pcCode);

        // Save plan → master CanopyPlan + N CanopyPlanDetails + 2 auto-REQs per row
        Task<SubmitCanopyPlanResponse> SubmitCanopyPlanAsync(
            SubmitCanopyPlanRequest req);

        // SP getcpyplandts_checker_maker — returns all candidate canopy parts
        // for the selected line (with per-PC KVA tier + stock + pending baked in).
        Task<List<CanopyPlanCheckerMakerRowDto>> GetCanopyPlanCheckerMakerRowsAsync(
            string lineWisePC);

        // ── Canopy Assembly Process (operator-side) ─────────────────
        // Canopy Type dropdown (SP LoadMachine — Foam / RockWool for canopy PCs)
        Task<List<CanopyProcessMachineDto>> GetCanopyProcessMachineListAsync(string pcCode);

        // KVA list for the selected machine + line (per-PC KVA tier applied)
        Task<List<CanopyProcessKvaDto>> GetCanopyProcessKvaListAsync(
            string machineCode, string pcCode);

        // Model list for the selected machine + KVA + line
        Task<List<CanopyProcessModelDto>> GetCanopyProcessModelListAsync(
            string machineCode, string kva, string pcCode);

        // Plan header for the picked machine + KVA + model (top-1 open PF row)
        Task<CanopyProcessPlanContextDto?> GetCanopyProcessPlanContextAsync(
            string machineCode, string kva, string model, string pcCode);

        // Kit picker rows (for PSH-mode already-open records)
        Task<List<CanopyProcessKitDto>> GetCanopyProcessKitListAsync(
            string machineCode, string pcCode, string planCode, string partCode);

        // Kit context (Bal remaining + PFB rate) after user picks a kit
        Task<CanopyProcessKitContextDto?> GetCanopyProcessKitContextAsync(
            string machineCode, string kitCode, string pcCode,
            string planCode, string partCode);

        // Part Details (kit parts to consume) — top HTML table
        Task<List<CanopyProcessPartRowDto>> GetCanopyProcessPartRowsAsync(
            string pcCode, int prcQty, string cpyPartCode,
            string planCode, string bomCode, string pfbCode);

        // Assembly Kit Details — bottom mat-table
        Task<List<CanopyProcessAssemblyKitRowDto>> GetCanopyProcessAssemblyKitRowsAsync(
            string pcCode, int prcQty, string cpyPartCode,
            string planCode, string bomCode, string pfbCode);

        // Save/End — transactional multi-table write. NEW path creates a
        // fresh PSH record; PSH path closes serial numbers and links files.
        Task<SubmitCanopyProcessResponse> SubmitCanopyProcessAsync(
            SubmitCanopyProcessRequest req);

        // ── Canopy Assembly Process Checker (quality review side) ───
        // Pending-list table: PSH records made by Makers with at least one
        // unit still QPCStatus='P' on the given LineWisePC.
        Task<List<CanopyProcessCheckPendingRowDto>> GetCanopyProcessCheckPendingListAsync(
            string pcCode);

        // Full detail for one PFB: header + kit lines + assembly-kit lines
        // + per-unit serial rows. Powers the modal.
        Task<CanopyProcessCheckContextDto?> GetCanopyProcessCheckContextAsync(
            string pfbCode);

        // Save per-unit decisions atomically. Soft reject in v1 — flips
        // ProcessFeedbackDetailsSub.QPCStatus and logs the activity.
        Task<SaveCanopyProcessCheckResponse> SaveCanopyProcessCheckAsync(
            SaveCanopyProcessCheckRequest request);

        // Date-range report for the Canopy Process Checker page. Returns every
        // PSH record on the given line whose Dt falls within [fromDate, toDate],
        // with per-decision unit counts for Excel export.
        Task<List<CanopyProcessCheckReportRowDto>> GetCanopyProcessCheckReportAsync(
            string pcCode, DateTime fromDate, DateTime toDate);
    }
}
