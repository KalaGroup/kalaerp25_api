using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.RequestDTO
{
    public class EngineDetailsRequest
    {
        public string SerialNo { get; set; }
        public string PartCode { get; set; }
        public string Category { get; set; }
        public string Stage { get; set; }
        public string PCCode { get; set; }
       
    }
}
