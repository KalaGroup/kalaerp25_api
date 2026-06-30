using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request
{
    /// <summary>Whole department's down time for one date, saved in one transaction.</summary>
    public class MachineDownTimeBatchRequest
    {
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd (from the form)
        public string CompanyCode { get; set; } = string.Empty;   // from session
        public string DeptCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }                     // session user id
        public List<MachineDownTimeEntry> Entries { get; set; } = new();
    }

    public class MachineDownTimeEntry
    {
        public string MachineCode { get; set; } = string.Empty;    // WKCode
        public string MachineName { get; set; } = string.Empty;
        public int Shift1Min { get; set; }
        public int Shift2Min { get; set; }
        public int LineShift1Min { get; set; }
        public int LineShift2Min { get; set; }
        public string? Status { get; set; }                        // "Open" / "Closed"
        public string? Remark { get; set; }
    }
}