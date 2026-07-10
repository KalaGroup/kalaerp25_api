namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    // Kit picker rows — only surfaces when the current plan header is in
    // PSH mode (already-open process). KitCode carries "PartDesc-->PartCode".
    public class CanopyProcessKitDto
    {
        public string KitDesc { get; set; } = string.Empty;   // AliseName
        public string KitCode { get; set; } = string.Empty;   // "PartDesc-->PartCode"
        public string PfbCode { get; set; } = string.Empty;
        public string EDt     { get; set; } = string.Empty;
    }
}
