using System;
using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request
{
    /// <summary>Whole department's manning for one date + shift, saved in one transaction
    /// into the 6MManpowerStatus (header) + 6MManpowerStatusDetails (lines) tables.</summary>
    public class ManpowerStatusBatchRequest
    {
        public string Date { get; set; } = string.Empty;          // yyyy-MM-dd (from the form)
        public string Shift { get; set; } = "F";                  // 'F' / 'S'
        public string CompanyCode { get; set; } = string.Empty;   // from session
        public int PcId { get; set; }                              // ProfitCenter.PC_ID
        public string PcCode { get; set; } = string.Empty;        // -> header.ProfitCenterCode
        public string? CreatedBy { get; set; }                     // session user id
        public List<ManpowerStatusEntry> Entries { get; set; } = new();
    }

    public class ManpowerStatusEntry
    {
        public string WkCode { get; set; } = string.Empty;
        public string WorkStationName { get; set; } = string.Empty; // ignored on save (resolved by join)
        // Sanctioned snapshot (frozen on first save; never overwritten afterwards). Float -> double.
        public double SancSkilled { get; set; }
        public double SancSemi { get; set; }
        public double SancUnskilled { get; set; }
        // Available — typed
        public double AvailSkilled { get; set; }
        public double AvailSemi { get; set; }
        public double AvailUnskilled { get; set; }
        public double Absent { get; set; }                         // manual: of the shortage, how many absent
        public string? Remark { get; set; }
    }
}