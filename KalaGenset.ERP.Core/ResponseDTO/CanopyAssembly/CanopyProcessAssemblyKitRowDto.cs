namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Rows for the bottom Assembly Kit table (mat-table in legacy UI).
    public class CanopyProcessAssemblyKitRowDto
    {
        public string Part     { get; set; } = string.Empty;   // "PartDesc-->PartCode"
        public double Qty      { get; set; }
        public double PrcQty   { get; set; }
        public double StkQty   { get; set; }
        public string PartCode { get; set; } = string.Empty;
    }
}
