using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class MOFAddPartDetailsResponseDTO
    {
        public int SrNo { get; set; }
        public string PartCode { get; set; }
        public string AdditionalPart { get; set; }
        public double Qty { get; set; }
        public double WIPStock { get; set; }
        public double Rate { get; set; }
    }
}
