using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public sealed class CpyPrcBendRequest
    {
        public string PCCode_Act { get; set; } = string.Empty;
        public string PCCode { get; set; } = string.Empty;
   
        public string PlanCode { get; set; } = string.Empty;
        public string CpyKitcode { get; set; } = string.Empty;
        public string CatID { get; set; } = string.Empty;
        public string PFBCode { get; set; } = string.Empty;
        public string MachineCodeSrNo { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string BOMcode { get; set; } = string.Empty;

        public string BatchQty { get; set; } = "0";
        public string PWt { get; set; } = "0";
        public string PSqft { get; set; } = "0";
        public string PFBRate { get; set; } = "0";
        public string EmpCode { get; set; } = string.Empty;
        public string Strokes { get; set; } = "0";

        public double PrcQty { get; set; }
        public double Rate { get; set; }

        /// <summary>Pipe/marker delimited attachment metadata: "machine-->file@#@machine-->file".</summary>
        public string AttachFileDts { get; set; } = string.Empty;

        /// <summary>Comma separated detail rows, each row "-->": part-->kitQty-->totQty-->...</summary>
        public string PrcDts { get; set; } = string.Empty;
    }
}
