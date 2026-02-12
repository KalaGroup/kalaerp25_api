using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Data.Models
{
    public class GetJobCardSrNo
    {
        public string JobCode { get; set; }
        public string Partcode { get; set; }
        public string SrNoPartcode { get; set; }
        public string SerialNo { get; set; }
        public int JPriority { get; set; }
        public string EPartcode { get; set; }
        public string Stage3Status { get; set; }
    }
}
