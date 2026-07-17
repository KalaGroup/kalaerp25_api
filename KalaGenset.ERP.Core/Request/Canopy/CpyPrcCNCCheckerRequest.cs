using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class CpyPrcCNCCheckerRequest
    {
        public string Status { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string PFBCode { get; set; } = string.Empty;

        /// <summary>Records joined by "@@#@@", each record's fields joined by "@#@".</summary>
        public string ProductionDetails { get; set; } = string.Empty;

        public string Sheetpartcode { get; set; } = string.Empty;
        public string PCCode_Act { get; set; } = string.Empty;
        public string PCCode { get; set; } = string.Empty;
        public string CatID { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string BatchQty { get; set; } = string.Empty;
        public string EmpCode { get; set; } = string.Empty;
        public string CompCode { get; set; } = string.Empty;
    }
}
