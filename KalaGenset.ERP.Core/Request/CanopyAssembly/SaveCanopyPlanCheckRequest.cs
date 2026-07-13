namespace KalaGenset.ERP.Core.Request.CanopyAssembly
{
    // Payload for POST /CanopyAssembly/SaveCanopyPlanCheck.
    // v1 ships Accept-only (matches the Canopy Process Checker convention);
    // Rework / Reject options are stubbed in the response shape but the
    // service currently only maps "Accept" to PlanStatus='D'.
    public class SaveCanopyPlanCheckRequest
    {
        public string EmpCode     { get; set; } = string.Empty;   // checker's EmpCode
        public string PCCode      { get; set; } = string.Empty;   // LineWisePC (audit only)
        public string ParentDgPC  { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string CPCode      { get; set; } = string.Empty;   // the plan being decided
        public string Decision    { get; set; } = "Accept";       // "Accept" | "Rework" | "Reject"
        public string Remark      { get; set; } = string.Empty;   // optional checker note
    }
}
