using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Data.Models
{
    public partial class PositionLineRight
    {
        public int PLRId { get; set; }

        public string PRMCode { get; set; }

        public string LineWisePC { get; set; }

        public string Active { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }
    }
}
