using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Jobcard
{
    /// <summary>
    /// Main request body for Job Card submission.
    /// Angular sends the full selected DG rows as a typed list —
    /// no delimiter string parsing needed.
    /// </summary>
    public class JobCardSubmitRequest
    {
        public string pcCode_Act { get; set; }
        public string pcCode_Old { get; set; }
        public string Remark { get; set; }
        public string EmpCode { get; set; }
        public List<JobCardDtsRow> Plans { get; set; } = new();
    }

    /// <summary>
    /// One DG model row selected by the user in the Angular table.
    /// Properties mirror the SP GetJobCardDGDts result columns.
    /// Angular binds the entire row and user fills Qty before submit.
    /// </summary>
    public class JobCardDtsRow
    {
        // ── Identifiers ──────────────────────────────────────────────
        public string? BOMCode { get; set; }   // Bill of Materials code
        public string? PartCode { get; set; }   // DG finished goods part code (1...)
        public string? PartDesc { get; set; }   // DG description

        // ── DG Specifications ────────────────────────────────────────
        public double? KVA { get; set; }   // Generator KVA rating
        public string? Phase { get; set; }   // 1 or 3 phase
        public string? Model { get; set; }   // DG model code
        public string? DGPanel { get; set; }   // Control panel / CFM type
        public string? PanelType { get; set; }   // Panel type identifier

        // ── Stock Counts (from SP — display only, not saved) ─────────
        public int? Eng { get; set; }   // Available engine serial nos
        public int? Alt { get; set; }   // Available alternator serial nos
        public int? Bat { get; set; }   // Available battery serial nos
        public int? Cpy { get; set; }   // Available canopy serial nos
        public int? BatLog { get; set; }   // Battery log store count
        public int? CpyLog { get; set; }   // Canopy log store count
        public int? DGStk { get; set; }   // DG finished goods stock
        public int? CPStk { get; set; }   // Control panel stock

        // ── Planning Fields ──────────────────────────────────────────
        public string? PlanCode { get; set; }   // Monthly plan code
        public string? PlanDate { get; set; }   // Planned production date
        public int? DayPlanQty { get; set; }   // Total qty planned for that date
        public int? FwPQty { get; set; }   // Already-created job card qty
        public int? PenPQty { get; set; }   // Pending qty = DayPlanQty - FwPQty

        // ── Production Metrics ───────────────────────────────────────
        public double? FNorm { get; set; }   // Floor norm (base + MTO)
        public int? PPlanQty { get; set; }   // Active job cards pending Stage1
        public int? PlReq { get; set; }   // Planning requirement = FNorm-(DStk+PPlanQty)
        public int? Stage3Qty { get; set; }   // Completed DGs in Stage 3
        public string? JobCard1Qty { get; set; }   // Job card 1 qty reference
        public string? Jobcard2Qty { get; set; }   // Job card 2 qty reference

        // ── Plan Calendar Info ───────────────────────────────────────
        public string? DayName { get; set; }   // e.g. "Monday"
        public string? TodayFlag { get; set; }   // "TODAY" or ""

        // ── User Input — filled by user in Angular table ─────────────
        public int Qty { get; set; }   // Qty to build (must be <= PenPQty)
    }
}
