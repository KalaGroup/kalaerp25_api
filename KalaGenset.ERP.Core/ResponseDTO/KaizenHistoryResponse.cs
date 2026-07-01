namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class KaizenHistoryResponse
    {
        public int Id { get; set; }
        public int KaizenSheetMasterId { get; set; }
        public string? KaizenSheetNo { get; set; }

        // Created | Resubmitted | SentBack | Authorized
        public string Action { get; set; } = null!;
        public string? Remark { get; set; }
        public string? ActionBy { get; set; }
        public string? ActionByCode { get; set; }

        // "yyyy-MM-dd HH:mm"
        public string ActionOn { get; set; } = null!;
    }
}
