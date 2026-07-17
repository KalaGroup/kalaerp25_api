using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request.Canopy
{
    public class CpyPrcCNCRequest
    {
        public string EmpCode { get; set; } = "";
        public string PCCode_Act { get; set; } = "";
        public string PCCode { get; set; } = "";
        public string PlanCode { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string TkitId { get; set; } = "";
        public string SheetPartcode { get; set; } = "";
        public int BatchQty { get; set; }
        public string MachineCodeSrNo { get; set; } = "";
        public int SerialNo { get; set; }
        public double ShQtyPerset { get; set; }
        public double ShWtperUts { get; set; }
        public double ShWtperSet { get; set; }
        public double ShWtperBatch { get; set; }

        // Format: Partcode,KitQty,TotQty,PfbRate,Plen,Pwidth,PThk,PLossWt,WtPeruts,SqftPerUts,catCode
        public string PrcDts { get; set; } = "";

        public string Remark { get; set; } = "";
        public string? AttachFileDts { get; set; }   // nullable - code checks IsNullOrEmpty
        public string CatID { get; set; } = "";
        public string OSSupplierCode { get; set; } = "";
        
    }
}
