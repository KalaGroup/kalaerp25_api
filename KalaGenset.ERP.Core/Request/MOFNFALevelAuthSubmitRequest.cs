using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.Request
{
    public class MOFNFALevelAuthSubmitRequest
    {
        public string MOFNo { get; set; }
        public string SaveType { get; set; }
        public string UserID { get; set; }
        public string AuthRemark { get; set; }
    }
}
