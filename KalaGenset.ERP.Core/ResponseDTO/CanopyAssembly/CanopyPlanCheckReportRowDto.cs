namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Row in the plan-checker's date-range Report table. Includes MakerCode +
    // aggregate detail counts so the Excel export can show planning throughput
    // per line for the selected period.
    public class CanopyPlanCheckReportRowDto
    {
        public string CPCode         { get; set; } = string.Empty;
        public string Dt             { get; set; } = string.Empty;
        public string FromDt         { get; set; } = string.Empty;
        public string ToDt           { get; set; } = string.Empty;
        public string PlanPCCode     { get; set; } = string.Empty;
        public string PlanType       { get; set; } = string.Empty;
        public string PlanStatus     { get; set; } = string.Empty;
        public string MakerCode      { get; set; } = string.Empty;
        public string CompanyCode    { get; set; } = string.Empty;
        public int    DetailRowCount { get; set; }
        public double TotalPlanQty   { get; set; }
        public double TotalWIPQty    { get; set; }   // aggregate CPYWIPQty across details
        public string KVAs           { get; set; } = string.Empty;   // distinct KVAs across the plan's detail parts
        public string PartCodes      { get; set; } = string.Empty;   // distinct Partcodes across the plan's detail rows
        public string Status         { get; set; } = string.Empty;   // "Pending" or "Authorized"
    }
}
