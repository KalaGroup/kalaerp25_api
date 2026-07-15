using System;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    /// <summary>Department dropdown item (ProfitCenter that has W1/W2/W3 sanctioned stations).</summary>
    public class ManpowerDeptDTO
    {
        public int PcId { get; set; }                              // ProfitCenter.PC_ID
        public string PcCode { get; set; } = string.Empty;
        public string PcName { get; set; } = string.Empty;
    }

    /// <summary>One station with its sanctioned headcount, split by skill (from the master).</summary>
    public class ManpowerStationDTO
    {
        public string WkCode { get; set; } = string.Empty;         // WorkStation.WkCode
        public string WorkStationName { get; set; } = string.Empty;
        public double SancSkilled { get; set; }                    // W3
        public double SancSemi { get; set; }                       // W2
        public double SancUnskilled { get; set; }                  // W1
    }

    /// <summary>
    /// One saved manning row for the View grid (Shortage = Sanctioned - Available, computed).
    /// Identity is (MCode, SrNo) — the detail key — used by the grid for edit/delete.
    /// Quantities are double because the detail Qty columns are float.
    /// </summary>
    public class ManpowerStatusRecordDTO
    {
        public string MCode { get; set; } = string.Empty;         // 6MManpowerStatus.MCode
        public int SrNo { get; set; }                              // 6MManpowerStatusDetails.SrNo
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd
        public string CompanyCode { get; set; } = string.Empty;    // owning company (LEFT(PCCode,2)) for the chart picker
        public string Shift { get; set; } = string.Empty;         // F / S
        public int PcId { get; set; }                              // resolved from ProfitCenter (by PcCode)
        public string PcName { get; set; } = string.Empty;
        public string WkCode { get; set; } = string.Empty;
        public string WorkStationName { get; set; } = string.Empty;
        public double SancSkilled { get; set; }
        public double SancSemi { get; set; }
        public double SancUnskilled { get; set; }
        public double AvailSkilled { get; set; }
        public double AvailSemi { get; set; }
        public double AvailUnskilled { get; set; }
        public double ShortSkilled { get; set; }
        public double ShortSemi { get; set; }
        public double ShortUnskilled { get; set; }
        public double Absent { get; set; }                         // of the shortage, how many are absent (manual)
        public string Remark { get; set; } = string.Empty;
    }

    /// <summary>Lightweight row for the Daily / Weekly / Monthly shortage charts (date range).</summary>
    public class ManpowerShortageTrendDTO
    {
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd
        public string CompanyCode { get; set; } = string.Empty;   // owning company (LEFT(PCCode,2)) for the chart picker
        public string PcName { get; set; } = string.Empty;
        public string WorkStationName { get; set; } = string.Empty;
        public double ShortTotal { get; set; }                     // (Sanc - Avail) summed over skills
        public double Absent { get; set; }
    }
}