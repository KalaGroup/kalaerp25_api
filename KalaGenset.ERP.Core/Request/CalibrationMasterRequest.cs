using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request
{
    public class CalibrationMasterRequest
    {
        public int CompanyId { get; set; }
        public string? MakerRemark { get; set; }
        public string? CheckerRemark { get; set; }
        public string Designation { get; set; } = "maker";
        public List<CalibrationEntryRequest> Entries { get; set; } = new List<CalibrationEntryRequest>();
    }

    public class CalibrationEntryRequest
    {
        public int InstrumentId { get; set; } = 0;
        public string partCode { get; set; }
        public string Type { get; set; }
        public string? IdNo { get; set; }
        public string? SrNo { get; set; }
        public string? Make { get; set; }
        public string? Range { get; set; }
        public string? Unit { get; set; }
        public string? LC { get; set; }
        public string? Location { get; set; }
        public DateTime? CalDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
