using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.RequestDTO
{
    public class Jobcard2SubmitDetails
    {
        public string PCCode { get; set; }
        public string PCCode_Act { get; set; }
        public string Remark { get; set; }
        // Operator code — sourced from localStorage.employeeCode on the UI side.
        // Populated on the audit-log write (InsertLoginTransactionDetails) so
        // each JobCard2 save can be attributed to a specific user.
        public string? EmpCode { get; set; }
        public List<ModelDetailDto> JobCard2Dts { get; set; }
    }

    public class ModelDetailDto
    {
        public string? BOMCode { get; set; }
        public int? CPStk { get; set; }
        public string? DGPanel { get; set; }
        public int? DGStk { get; set; }
        public int? JobCard1Qty { get; set; }
        public double? KVA { get; set; }
        public string? Model { get; set; }
        public string? PanelType { get; set; }
        public string? PartCode { get; set; }
        public string? Phase { get; set; }
        public int? Stage3Qty { get; set; }
        public string? Jobcard2Qty { get; set; }
    }
}
