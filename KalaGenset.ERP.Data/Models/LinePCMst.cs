using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Data.Models
{
    public partial class LinePCMst
    {
        public string LineWisePC { get; set; }

        public string LineDesc { get; set; }

        public string ParentDgPC { get; set; }

        public int? DivisionId { get; set; }

        public string Active { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }
    }
}
