using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class PanelTypePartcodeDto
    {
        public string PanelTypePartcode { get; set; }

        public string SerialNo { get; set; }
        public string GCode { get; set; }
        public string PartCode { get; set; }
        public DateTime? QDt { get; set; }
        public string TRFStatus { get; set; }
    }
}
