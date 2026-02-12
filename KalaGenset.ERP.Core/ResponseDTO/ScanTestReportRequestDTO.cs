using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class ScanTestReportRequestDTO
    {
        public string SrNo { get; set; }
        public string DGSrNo { get; set; }
        public string Category { get; set; }
        public string PCCode { get; set; }
    }
}
