namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // One row returned by the stored procedure getcpyplandts_checker_maker.
    // The SP already applies the per-PC KVA tier (1-58.5 / 58.5-250 / 250+)
    // and computes StkQty + PendQty per part.
    public class CanopyPlanCheckerMakerRowDto
    {
        public string BOMCode  { get; set; } = string.Empty;
        public string PartDesc { get; set; } = string.Empty;   // "<desc>-->" + PartCode
        public string PartCode { get; set; } = string.Empty;
        public string UName    { get; set; } = string.Empty;
        public double KVA      { get; set; }
        public double StkQty   { get; set; }
        public double PendQty  { get; set; }
    }
}
