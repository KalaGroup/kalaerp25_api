using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class LineDto
    {
        public string LineWisePC { get; set; }
        public string LineDesc { get; set; }
        public string ParentDgPC { get; set; }
    }
}
