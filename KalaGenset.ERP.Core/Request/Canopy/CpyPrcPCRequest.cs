using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class CpyPrcPCRequest
    {
        public string EmpCode { get; set; } = string.Empty;
        public string PCCode_Act { get; set; } = string.Empty;
        public string PCCode { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public string MachineCodeSrNo { get; set; } = string.Empty;
        public int StdSqft { get; set; }
        public string PrcDts { get; set; } = string.Empty;  //item.PlanCode + "-->" +item.ProductCode + "-->"  +  item.BOMCode+ "-->" + item.KitCode+ "-->" +  item.BatchQty+ "-->" + item.Sqft+ "-->" + item.PrcQty + "-->" + item.PFBCode+ "-->" + item.EDT;
        public string Remark { get; set; } = string.Empty;
        public string CatID { get; set; } = string.Empty;
        public string AttachFileDts { get; set; } = string.Empty;
    }
}
