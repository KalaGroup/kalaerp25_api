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
    }
}
