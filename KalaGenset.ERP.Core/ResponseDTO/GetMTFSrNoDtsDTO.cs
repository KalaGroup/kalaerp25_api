using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public  class GetMTFSrNoDtsDTO
    {
        public string PartDesc { get; set; }

        public string PartCode { get; set; }

        public string UName { get; set; }

        public string CatId { get; set; }

        public string Catname { get; set; }

        public string SerialNo { get; set; }
    }
}
