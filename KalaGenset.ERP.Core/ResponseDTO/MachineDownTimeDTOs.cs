using System;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    /// <summary>Department dropdown item (from AssignMachineToPC + ProfitCenter).</summary>
    public class MachineDeptDTO
    {
        public string DeptCode { get; set; } = string.Empty;       // ProfitCenter.PCCode
        public string DeptName { get; set; } = string.Empty;
    }

    /// <summary>Machine dropdown / grid item (from AssignMachineToPC).</summary>
    public class MachineDTO
    {
        public string MachineCode { get; set; } = string.Empty;    // PartCode
        public string MachineName { get; set; } = string.Empty;    // AliseSerialNo
    }

    /// <summary>
    /// One saved down-time row for the View grid.
    /// Identity is (MCode, SrNo) — the detail key — used by the grid for edit/delete.
    /// </summary>
    public class MachineDownTimeRecordDTO
    {
        public string MCode { get; set; } = string.Empty;         // 6MMachineDownTime.MCode
        public int SrNo { get; set; }                              // 6MMachineDownTimeDetails.SrNo
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd
        public string DeptCode { get; set; } = string.Empty;      // = header.ProfitCenterCode
        public string DeptName { get; set; } = string.Empty;
        public string MachineCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        // Machine down time
        public int Shift1Min { get; set; }
        public int Shift2Min { get; set; }
        public int TotalMin { get; set; }                          // Shift1 + Shift2 (computed)
        // Line down time (split by shift)
        public int LineShift1Min { get; set; }
        public int LineShift2Min { get; set; }
        public int LineTotalMin { get; set; }                      // LineShift1 + LineShift2 (computed)
        public string Status { get; set; } = string.Empty;        // "Open" / "Closed"
        public string Remark { get; set; } = string.Empty;
    }

    /// <summary>Lightweight row for the Daily / Weekly / Monthly trend charts (date range).</summary>
    public class MachineDownTimeTrendDTO
    {
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd
        public string DeptName { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public int TotalMin { get; set; }                          // machine down time (computed)
        public int LineTotalMin { get; set; }                      // line down time (computed)
        public string Status { get; set; } = string.Empty;
    }
}