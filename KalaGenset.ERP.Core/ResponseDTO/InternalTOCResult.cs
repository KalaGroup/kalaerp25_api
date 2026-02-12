using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class InternalTOCResult
    {
        public string Partcode { get; set; }         // Assuming Partcode is a string
        public decimal MOQ { get; set; }             // Decimal type matches SQL DECIMAL/NUMERIC
        public decimal Poper { get; set; }           // Decimal type for precise calculations
        public decimal Stk { get; set; }             // Stock balance
        public decimal PndReq { get; set; }          // Pending request quantity
        public decimal Req { get; set; }             // Required quantity
        public int Flag { get; set; }                // Floor function result (integer)
        public decimal RaiseReqQty { get; set; }     // Raised request quantity
        public string FromPC { get; set; }           // Profit Center Code
        public string ToPCCode
        {
            get; set;
        }
    }
}
