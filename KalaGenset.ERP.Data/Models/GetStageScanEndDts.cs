using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Data.Models
{
    public class GetStageScanEndDts
    {
        public string JobCode { get; set; }
        public string JobDt { get; set; }
        public string DgProductCode { get; set; }
        public double DGS1Stk { get; set; }
        public string DgProductDesc { get; set; }
        public string EngPartCode { get; set; }
        public string EngPartDesc { get; set; }
        public int JPriority { get; set; }
        public string AltDts { get; set; }
        public string Stage1Status { get; set; }
        
    }
}
