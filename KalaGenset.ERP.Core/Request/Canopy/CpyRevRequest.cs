using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class CpyRevRequest
    {
        public string PCCode { get; set; }
        public string PCCode_Act { get; set; }
        public string TransType { get; set; }   // 'IndividualCode' | 'AllCode'
        public string EmpCode { get; set; }

        /// Replaces the legacy "RevPrcDts" string: "CP-->Part-->Cat,CP-->Part-->Cat"
        public List<CpyRevDetail> Details { get; set; }
   
        /// Structured 6M rows — saved to CpyRevTrans6MDts, one row per item.
        public List<ProductionDetail> ProductionDetails { get; set; }
    }

    public class CpyRevDetail
    {
        public string CPCode { get; set; }   // legacy Dts[0] — CanopyPlanCode
        public string ProductCode { get; set; }   // legacy Dts[1]
        public string CatId { get; set; }   // legacy Dts[2]
    }

        public class ProductionDetail
    {
        public int Id { get; set; }
        public string SixM { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssignTo { get; set; } = string.Empty;
        public string EmpPCCode { get; set; } = string.Empty;

    }

}

