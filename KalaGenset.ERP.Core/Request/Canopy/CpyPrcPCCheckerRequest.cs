using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class CpyPrcPCCheckerRequest
    {

        public string Code { get; set; } = string.Empty;
        public string CompCode { get; set; } = string.Empty;
        public string EmpCode { get; set; } = string.Empty;
        public string PCCode_Act { get; set; } = string.Empty;
        public string PCCode { get; set; } = string.Empty;
        public string PFBCode { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string Sheetpartcode { get; set; } = string.Empty;
        public string CatID { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string BatchQty { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string ProductionDetails { get; set; } = string.Empty;
        public string ProductionType { get; set; } = string.Empty;
        public List<ProductionDetail> Details { get; set; }
        public class ProductionDetail
        {
            public int Id { get; set; }  
            public string SixM { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string AssignTo { get; set; } = string.Empty;
            public string EmpPCCode { get; set; } = string.Empty;

        }

    }
}
