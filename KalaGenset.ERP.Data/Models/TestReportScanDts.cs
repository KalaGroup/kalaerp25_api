using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Data.Models
{
    public class TestReportScanDts
    {
        public double KVA { get; set; }
        public string? PFBCode { get; set; }
        public string? Dt { get; set; }
        public string? SerialNo { get; set; }
        public string? Partcode { get; set; }
        public string? Partdesc { get; set; }
        public string? PanelType { get; set; }
        public string? Engdts { get; set; }
        [Column("Altdts")]
        public string? Altdts { get; set; }
        public double? DieselQty { get; set; }
        public string? DieselPart { get; set; }
        public double? DieselRate { get; set; }
        public string? TRCode { get; set; }
        public string? TRStartTime { get; set; }
        public string? TREndTime { get; set; }
        public string? DGStartTime { get; set; }
        public string? DGEndTime { get; set; }
        public string? QAStatus { get; set; }
        public string? TRStatus { get; set; }

    }
}
