using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request
{
    public class MTFScanSubmitRequest
    {
        public string MtfCode { get; set; }
        public List<MTFSerialNoDtl> MTFSerialNoDts { get; set; }

    }

    public class MTFSerialNoDtl
    {
        public string Partcode { get; set; }
        public string SerialNo { get; set; }
    }

}
