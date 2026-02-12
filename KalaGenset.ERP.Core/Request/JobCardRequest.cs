using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.RequestDTO
{
    public class JobCardRequest
    {
        public string? Type { get; set; }
        public string? Code { get; set; }
        public string? FromDt { get; set; }
        public string? ToDt { get; set; }
    }
}
