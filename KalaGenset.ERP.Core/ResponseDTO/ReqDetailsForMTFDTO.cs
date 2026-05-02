using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class ReqDetailsForMTFDTO
    {
        public string PartDesc { get; set; }
        public string PartCode { get; set; }
        public string UName { get; set; }
        public string Uid { get; set; }
        public double KitQty { get; set; }
        public double ReqQty { get; set; }
        public double PQty { get; set; }
        public double Stk { get; set; }
        public double MTFQty { get; set; }
        public double QtyAfterMTF { get; set; }
        public double Rate { get; set; }
        public double Amt { get; set; }
        public double SheetQty { get; set; }
        public string ConvUOMCode { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public string MOB { get; set; }
    }
}
