namespace KalaGenset.ERP.Core.Request.CanopyAssembly
{
    // Input for the Search button on the Flat Pack Canopy Assembly Process page.
    // Maps to legacy BindDetails(): runs the right sp_GetFlatPackProcessDetails*
    // variant and returns the grid + Rate/Wt/SqFt/CR/HR side-fields.
    public class FlatPackProcessDetailsRequest
    {
        public string PCCode { get; set; } = string.Empty;          // selected line/PC
        public string CanopyPartCode { get; set; } = string.Empty;  // ddlCanopyPartDesc.Value
        public string PartCode { get; set; } = string.Empty;        // PartCode parsed from BindPrimary's PartDesc-->PartCode
        public string ProcessType { get; set; } = string.Empty;     // "CPY" or "CPY(BF_FT)"
        public double ProcessQty { get; set; }
    }
}
