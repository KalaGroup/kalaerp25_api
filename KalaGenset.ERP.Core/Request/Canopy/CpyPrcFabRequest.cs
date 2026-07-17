using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class CpyPrcFabRequest
    {
        public string EmpCode { get; set; } = string.Empty;
        public string PCCode_Act { get; set; } = string.Empty;
        public string PCCode { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string PFBCode { get; set; } = string.Empty;
        public string CpyKitcode { get; set; } = string.Empty;
        public int BatchQty { get; set; }
        public int PrcQty { get; set; }
        public string MachineCodeSrNo { get; set; } = string.Empty;
        public string OSSupplierCode { get; set; } = string.Empty;
        public string BOMcode { get; set; } = string.Empty;
        public double PFBRate { get; set; }
        public string PrcDts { get; set; } = string.Empty;  // Partcode,KitQty,TotQty,PfbRate,WtPeruts,
        public string Remark { get; set; } = string.Empty;
        public double Rate { get; set; }
        public string AttachFileDts { get; set; } = string.Empty;
        public double PWt { get; set; }
        public double PSqft { get; set; }
        public string CatID { get; set; } = string.Empty;
    }
}
