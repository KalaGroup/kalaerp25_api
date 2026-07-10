namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Row in the checker's date-range Report table. Same shape as the pending
    // list but carries per-decision unit counts (Accepted / Rework / Rejected)
    // so the Excel export can show quality outcomes at a glance.
    public class CanopyProcessCheckReportRowDto
    {
        public string PFBCode          { get; set; } = string.Empty;
        public string Dt               { get; set; } = string.Empty;
        public string ProductCode      { get; set; } = string.Empty;
        public string ProductDesc      { get; set; } = string.Empty;
        public double KVA              { get; set; }
        public string Model            { get; set; } = string.Empty;
        public double BatchQty         { get; set; }
        public double PrcQty           { get; set; }
        public string MachineCode      { get; set; } = string.Empty;
        public string SerialNo         { get; set; } = string.Empty;
        public string MakerCode        { get; set; } = string.Empty;
        public string PlanCode         { get; set; } = string.Empty;
        public string BOMCode          { get; set; } = string.Empty;

        // Unit-level counts sourced from ProcessFeedbackDetailsSub.
        public int TotalUnitCount    { get; set; }
        public int PendingUnitCount  { get; set; }
        public int AcceptedCount     { get; set; }   // QPCStatus = 'D'
        public int ReworkCount       { get; set; }   // QPCStatus = 'RW'
        public int RejectedCount     { get; set; }   // QPCStatus = 'R'
        public int DecidedUnitCount  { get; set; }   // Accepted + Rework + Rejected

        public string Status         { get; set; } = string.Empty;   // "Pending" or "Authorized"
    }
}
