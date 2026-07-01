namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // One row in the Canopy Part Description dropdown — lazy-loaded as the
    // user types. Mirrors the legacy ddlPartDesc_LoadingItems query output.
    public class CanopyPlanPartOptionDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartDesc { get; set; } = string.Empty;   // "<desc>-->" + PartCode
        public string BomCode  { get; set; } = string.Empty;
        public string UName    { get; set; } = string.Empty;
    }
}
