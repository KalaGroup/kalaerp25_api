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
    }
}
