using System.Collections.Generic;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class QualityCheckListReportRowDTO
    {
        public int StageWiseQcid { get; set; }
        public string? Pccode { get; set; }
        public string? PCName { get; set; }
        public string? StageName { get; set; }
        public decimal FromKva { get; set; }
        public decimal ToKva { get; set; }
        public string? MakerRemark { get; set; }
        public string? CheckerAuthRemark { get; set; }
        public bool IsActive { get; set; }
        public bool IsAuth { get; set; }
        public bool IsDiscard { get; set; }
        public int ItemCount { get; set; }
        public string? AuthStatus { get; set; }
        public List<QualityCheckListItemDTO> Items { get; set; } = new();
    }

    public class QualityCheckListItemDTO
    {
        public int StageWiseQcdetailId { get; set; }
        public int SrNo { get; set; }
        public string? SubAssemblyPart { get; set; }
        public string? QualityProcessCheckpoint { get; set; }
        public string? Specification { get; set; }
        public string? Observation { get; set; }
        public string? OkNok { get; set; }
    }
}
