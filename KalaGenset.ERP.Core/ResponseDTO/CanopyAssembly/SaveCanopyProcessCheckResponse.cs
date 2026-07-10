namespace KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly
{
    public class SaveCanopyProcessCheckResponse
    {
        public string Message        { get; set; } = string.Empty;
        public string PFBCode        { get; set; } = string.Empty;
        public int    AcceptedCount  { get; set; }
        public int    ReworkCount    { get; set; }
        public int    RejectedCount  { get; set; }
    }
}
