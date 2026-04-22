using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Jobcard
{
    public class Jobcard1CheckerSubmitRequest
    {
        public string? EmpCode { get; set; }
        public string? PCCode_Act { get; set; }
        public string? PCCode_Old { get; set; }
        public string? JobCode { get; set; }
        public string? Status { get; set; }  // "Auth" or "Reject"
        public List<CheckerDetailItem> Details { get; set; }
    }

    public class CheckerDetailItem
    {
        public string SixM { get; set; }
        public string Description { get; set; }
        public string AssignTo { get; set; }
        public string AssignName { get; set; }
        public bool Selected { get; set; }
    }
}
