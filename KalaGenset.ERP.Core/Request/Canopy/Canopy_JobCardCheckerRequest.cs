using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class Canopy_JobCardCheckerRequest
    {

        public string Code { get; set; }
        public string CompCode { get; set; }
        public string EmpCode { get; set; }
        public string PCCode { get; set; }
        public int BatchQty { get; set; }
        public float Kva { get; set; }
        public string Model { get; set; }
        public string PlanCode { get; set; }
        public string Partcode { get; set; }
        public string bomCode { get; set; }
        public string ProductionDetails { get; set; }
        public string ProductionType { get; set; }
        public string Status { get; set; }
        public List<ProductionDetail> Details { get; set; }
        public class ProductionDetail
        {
            public int Id { get; set; }
            public string SixM { get; set; }
            public string Description { get; set; }
            public string AssignTo { get; set; }
            public string EmpPCCode { get; set; }

        }

      
    }
}
