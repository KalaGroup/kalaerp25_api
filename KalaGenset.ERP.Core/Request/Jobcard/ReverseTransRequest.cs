using System.Collections.Generic;

namespace KalaGenset.ERP.Core.Request.Jobcard
{
    public class ReverseTransRequest
    {
        public string PCCode { get; set; } = string.Empty;
        public int RevTransFor { get; set; }
        public string Remark { get; set; } = string.Empty;

        // One entry per ticked row in the search-results table.
        public List<ReverseTransRow> Rows { get; set; } = new();
    }

    public class ReverseTransRow
    {
        // Field order matches the legacy Dts[0..7] mapping; names match
        // the SP output / UI ReverseTransSearchResult interface so the UI
        // can post the typed object straight through.
        public string? EngSrNo { get; set; }    // Dts[0]
        public string? JobCode { get; set; }    // Dts[1]
        public string? J2Priority { get; set; } // Dts[2]
        public string? Partcode { get; set; }   // Dts[3]
        public string? JobCard1 { get; set; }   // Dts[4]
        public string? PanelType { get; set; }  // Dts[5]
        public string? Stage4Code { get; set; } // Dts[6]
        public string? TRCode { get; set; }     // Dts[7]
    }
}
