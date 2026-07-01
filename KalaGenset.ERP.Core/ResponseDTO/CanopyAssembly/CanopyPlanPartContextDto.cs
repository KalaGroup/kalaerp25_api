namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Returned by GetCanopyPlanPartContext — the 4 scalars the UI needs after
    // a part is picked. Replaces the legacy GetPartCode WebMethod which
    // returned an untyped List<string> in positional order.
    public class CanopyPlanPartContextDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string BomCode  { get; set; } = string.Empty;
        public double StkQty   { get; set; }
        public double PendQty  { get; set; }
    }
}
