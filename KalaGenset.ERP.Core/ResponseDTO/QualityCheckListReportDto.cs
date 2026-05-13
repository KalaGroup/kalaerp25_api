using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class QualityCheckListReportDto
    {
        public int     StageWiseQcid     { get; set; }
        public string  Pccode            { get; set; }
        public string  PCName            { get; set; }
        public string  StageName         { get; set; }
        public decimal FromKva           { get; set; }
        public decimal ToKva             { get; set; }
        public string? MakerRemark       { get; set; }
        public string? CheckerAuthRemark { get; set; }
        public bool    IsActive          { get; set; }
        public bool    IsAuth            { get; set; }
        public bool    IsDiscard         { get; set; }
        public int     ItemCount         { get; set; }
        // Derived: "Authorized" | "Pending" | "Inactive" | "Discarded"
        public string  AuthStatus        { get; set; }
        // Full checkpoint detail rows for this checklist (ordered by SrNo)
        public List<QualityCheckListItemDto> Items { get; set; } = new List<QualityCheckListItemDto>();
    }
}
