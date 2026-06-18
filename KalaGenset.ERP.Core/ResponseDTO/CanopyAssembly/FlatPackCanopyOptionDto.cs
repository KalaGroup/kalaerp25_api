namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // One row in the "Canopy Part Desc" dropdown on the Flat Pack page.
    // Sourced from PartToCDetailsSupplier ∪ MTODts (see legacy
    // BindDDLCanopyPartDesc).
    public class FlatPackCanopyOptionDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartDesc { get; set; } = string.Empty;   // "<desc>-->" + PartCode
        public string Kva      { get; set; } = string.Empty;
        public string Model    { get; set; } = string.Empty;
        public string Phase    { get; set; } = string.Empty;
        public string Type     { get; set; } = string.Empty;
    }
}
