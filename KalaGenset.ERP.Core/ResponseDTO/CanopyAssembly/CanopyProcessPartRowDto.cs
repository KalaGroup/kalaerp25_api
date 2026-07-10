namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Rows for the "Part Details" table (top HTML table in legacy UI). Carries
    // both the displayable Part / KitQty / PrcQty / StkQty AND the hidden
    // dimensions (Wt / TotWt / Sqft / TotSqft / Rate / PartCode) that Save
    // needs when composing PrcDts. New UI keeps them structured, not in the DOM.
    public class CanopyProcessPartRowDto
    {
        public string Part     { get; set; } = string.Empty;   // AliseName or "PartDesc-->PartCode"
        public double KitQty   { get; set; }
        public double PrcQty   { get; set; }
        public double StkQty   { get; set; }
        public double Wt       { get; set; }
        public double TotWt    { get; set; }
        public double Sqft     { get; set; }
        public double TotSqft  { get; set; }
        public double Rate     { get; set; }
        public string PartCode { get; set; } = string.Empty;
    }
}
