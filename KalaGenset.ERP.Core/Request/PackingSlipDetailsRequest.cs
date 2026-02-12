using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.RequestDTO
{
    public class PackingSlipDetailsRequest
    {
        public string? strSrNo { get; set; }
        public string? StrDGSrNo { get; set; }
        public string? strCat { get; set; }
        public string? strCPBatCnt { get; set; }
        public string? StrPCCode { get; set; }
    }
}
