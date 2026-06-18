using System.Collections.Generic;
using KalaGenset.ERP.Core.ResponseDTO.CanopyAssembly;

namespace KalaGenset.ERP.Core.Request.CanopyAssembly
{
    // Input for the Save button on the Flat Pack Canopy Assembly Process page.
    // Mirrors the legacy setSave("S") path: master ProcessFeedBack + per-row
    // ProcessFeedBackDetails + StockWIP + (for CPY) ProcessFeedbackDetailsSub
    // serials + (for PC=01.093) Kanban auto-REQ + login transaction logs.
    public class FlatPackSubmitRequest
    {
        public string PCCode { get; set; } = string.Empty;          // ProfitCenterCode (selected line)
        public string CompanyCode { get; set; } = string.Empty;     // "01"
        public string EmpCode { get; set; } = string.Empty;         // for LoginTransactionDetails
        public string ProcessType { get; set; } = string.Empty;     // "CPY" or "CPY(BF_FT)"
        public string CanopyPartCode { get; set; } = string.Empty;  // ProductCode = CanopyCode
        public string PartCode { get; set; } = string.Empty;        // NestingForCode (PartCode parsed from PartDesc)
        public double ProcessQty { get; set; }
        public string BomCode { get; set; } = string.Empty;         // TurretKitCode
        public string Heading { get; set; } = string.Empty;         // CpyStageType ("Line1"/"Line4")

        // Master rate + weight aggregates from BindDetails
        public double OverallRate { get; set; }
        public double Wt { get; set; }
        public double SqFt { get; set; }
        public double CRWt { get; set; }
        public double HRWt { get; set; }
        public double CRRate { get; set; }
        public double HRRate { get; set; }

        // Grid rows the user saw — same shape returned by GetProcessDetails.
        // Server re-validates these row-by-row before committing.
        public List<FlatPackPartDetailRow> PartDetails { get; set; } = new();
    }
}
