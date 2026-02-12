using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class PartDescOfMTFScanDTO
    {
        public string KitPartDesc { get; set; }

        public string KitPartCode { get; set; }

        public double MTFQty { get; set; }
    }
}
