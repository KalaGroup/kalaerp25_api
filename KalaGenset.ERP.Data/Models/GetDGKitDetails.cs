using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Data.Models
{
    public  class GetDGKitDetails
    {
        public string? PartCode { get; set; }
        public string? PartDesc { get; set; }
        public string? SerialNo { get; set; } = ""; // Always empty in SQL
        public string? UName { get; set; }
        public double? Qty { get; set; }
        public string? Mob { get; set; }
        public bool Kit { get; set; } 
        public double ConversionValue { get; set; } 
        public string? UOMCode { get; set; } 
        public string? StockQty { get; set; } 
        public string? TotalQty { get; set; }
        public double Rate { get; set; }
        public double QAP { get; set; }
        public double Amount { get; set; }
        public string? CategoryID { get; set; } 
    }
}
