using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request.CanopyAssembly
{
    // Payload for POST /CanopyAssembly/SaveCanopyProcessCheck.
    // Per-unit split: each unit in `Decisions` gets its own Accept / Rework /
    // Reject verdict (soft reject in v1 — flips QPCStatus only, doesn't return
    // serials to the pool or reverse WIP).
    public class SaveCanopyProcessCheckRequest
    {
        public string EmpCode      { get; set; } = string.Empty;   // checker's EmpCode
        public string PCCode       { get; set; } = string.Empty;   // LineWisePC
        public string ParentDgPC   { get; set; } = string.Empty;
        public string CompanyCode  { get; set; } = string.Empty;
        public string PFBCode      { get; set; } = string.Empty;
        public string ProductCode  { get; set; } = string.Empty;
        // Plan + Batch info — needed by the Checker to run the Kanban trigger
        // that was previously in the Maker's Start path (moved so Kanban REQs
        // only fire after QC has authorized units, not at Start time).
        public string PlanCode     { get; set; } = string.Empty;
        public double BatchQty     { get; set; }
        public List<CanopyProcessCheckUnitDecision> Decisions { get; set; } = new();
    }

    // One decision per unit (serial number). Decision drives the QPCStatus flip:
    //   Accept -> 'D'   (approved, moves out of pending pool)
    //   Rework -> 'RW'  (needs rework — 6M / RaiseESP typically filled)
    //   Reject -> 'R'   (soft reject — v1 doesn't return serial to pool)
    public class CanopyProcessCheckUnitDecision
    {
        public string SerialNo { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;   // "Accept" | "Rework" | "Reject"
        public string SixM     { get; set; } = string.Empty;   // "None" | "Man" | "Machine" | "Material" | "Method" | "Measurement" | "Environment"
        public string RaiseESP { get; set; } = string.Empty;   // employee code, only meaningful when 6M != None
        public string Remark   { get; set; } = string.Empty;
    }
}
