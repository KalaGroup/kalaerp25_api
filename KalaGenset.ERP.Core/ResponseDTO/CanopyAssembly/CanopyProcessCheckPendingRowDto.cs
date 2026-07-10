namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // One row in the checker's list — every PSH record for the line, with a
    // Status flag showing whether the checker still has work to do on it.
    // Once all units are decided (QPCStatus IN ('D','RW','R')), the row stays
    // visible with Status='Authorized' so the operator can see per-line
    // throughput at a glance.
    public class CanopyProcessCheckPendingRowDto
    {
        public string PFBCode          { get; set; } = string.Empty;
        public string Dt               { get; set; } = string.Empty;
        public string ProductCode      { get; set; } = string.Empty;
        public string ProductDesc      { get; set; } = string.Empty;   // "PartDesc-->PartCode"
        public double KVA              { get; set; }
        public string Model            { get; set; } = string.Empty;
        public double BatchQty         { get; set; }
        public double PrcQty           { get; set; }
        public string MachineCode      { get; set; } = string.Empty;
        public string SerialNo         { get; set; } = string.Empty;   // machine serial (Foam1/RockWool1)
        public string MakerCode        { get; set; } = string.Empty;   // maker's EmpCode (PPWCode)
        public int    TotalUnitCount   { get; set; }                    // all units for this PFB
        public int    DecidedUnitCount { get; set; }                    // units with QPCStatus IN ('D','RW','R')
        public int    PendingUnitCount { get; set; }                    // still awaiting checker
        public string Status           { get; set; } = string.Empty;    // "Pending" or "Authorized"
    }
}
