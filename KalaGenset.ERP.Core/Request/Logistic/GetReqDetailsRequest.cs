using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Logistic
{
    public class GetReqDetailsRequest
    {
        public string PCCode { get; set; }
        public string StrBomCode { get; set; }
        public string StrReqCode { get; set; }
        public double StrReqQty { get; set; }
        public double StrMTFQty { get; set; }
    }
}
