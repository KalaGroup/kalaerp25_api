namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // One row in the Canopy Plan Checker's list — every CanopyPlan for the
    // line with a Status flag showing whether the checker still has work to
    // do on it. Once PlanStatus flips to 'D' (Done/Authorized), the row
    // stays visible with Status='Authorized' so operators can see per-line
    // planning throughput at a glance.
    public class CanopyPlanCheckPendingRowDto
    {
        public string CPCode        { get; set; } = string.Empty;
        public string Dt            { get; set; } = string.Empty;   // plan created date
        public string FromDt        { get; set; } = string.Empty;   // plan window from
        public string ToDt          { get; set; } = string.Empty;   // plan window to
        public string PlanPCCode    { get; set; } = string.Empty;   // LineWisePC
        public string PlanType      { get; set; } = string.Empty;   // 'M' = manual, 'G' = generated
        public string PlanStatus    { get; set; } = string.Empty;   // 'P' / 'D'
        public string MakerCode     { get; set; } = string.Empty;   // plan author's EmpCode (if tracked)
        public string CompanyCode   { get; set; } = string.Empty;
        public int    DetailRowCount { get; set; }                   // number of CanopyPlanDetails rows
        public double TotalPlanQty  { get; set; }                    // sum of Qty across details
        public string KVAs          { get; set; } = string.Empty;   // distinct KVAs across the plan's detail parts (e.g. "10, 15, 25")
        public string PartCodes     { get; set; } = string.Empty;   // distinct Partcodes across the plan's detail rows
        public string Status        { get; set; } = string.Empty;   // "Pending" or "Authorized"
    }
}
