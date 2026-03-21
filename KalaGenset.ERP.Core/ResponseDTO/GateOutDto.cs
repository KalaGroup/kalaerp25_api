using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class GateOutDto
    {
        public string MOFCode { get; set; }
        public string InvId { get; set; }
        public string Dt { get; set; }
        public string CustName { get; set; }
        public string CustAddress { get; set; }
        public string PartDesc { get; set; }
        public string PCName { get; set; }
        public string VehicleNo { get; set; }
        public string DriverName { get; set; }
        public string DriverMobileNo { get; set; }
        public string HODMailID { get; set; }
        public string CCMailID { get; set; }
        public string ReplyToMailID { get; set; }
        public string OrderBy { get; set; }
    }
}
