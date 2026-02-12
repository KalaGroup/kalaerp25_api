using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class GetMTFDetailsResponseDTO
    {
        public string FPCCode { get; set; }

        public string TPCCode { get; set; }

        public string Partcode { get; set; }

        public double IssueQty { get; set; }    

    }
}
