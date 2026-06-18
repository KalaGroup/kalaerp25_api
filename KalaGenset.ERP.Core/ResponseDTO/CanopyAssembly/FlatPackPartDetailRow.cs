namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // One row of the Part Details grid — output of sp_GetFlatPackProcessDetails
    // (CPY) or sp_GetFlatPackProcessDetailsCPY (BF+FT). Same shape echoed back
    // by the client at Save so we re-validate stock.
    public class FlatPackPartDetailRow
    {
        public string PartCode { get; set; } = string.Empty;   // legacy: "PartDesc-->PartCode"
        public double Qty { get; set; }                        // KitQty
        public string UName { get; set; } = "Nos";             // always "Nos" in legacy
        public double Rate { get; set; }
        public double TotalQty { get; set; }                   // Qty * ProcessQty
        public double Stk { get; set; }
        public double QtyAfterProcess { get; set; }            // Stk - TotalQty
        public double Amount { get; set; }                     // TotalQty * Rate
    }
}
