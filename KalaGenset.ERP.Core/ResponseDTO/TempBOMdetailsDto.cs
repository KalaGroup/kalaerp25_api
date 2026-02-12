using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class TempBOMdetailsDto
    {
        public string PartCode { get; set; }
        public string PartDesc { get; set; }
        public string SerialNo { get; set; }
        public string UName { get; set; }
        public float Qty { get; set; }
        public string MOB { get; set; }
        public string Kit { get; set; }
        public float ConversionValue { get; set; }
        public string UOMCode { get; set; }
        public float StockQty { get; set; }
        public float TotalQty { get; set; }
        public float Rate { get; set; }
        public float QAP { get; set; }
        public float Amount { get; set; }
        public int CategoryID { get; set; }
    }
}
