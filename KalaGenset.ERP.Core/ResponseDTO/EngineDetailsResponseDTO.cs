using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class EngineDetailsResponseDTO
    {
        public string JobCode { get; set; }
        public string JobDt { get; set; }
        public string DgProductCode { get; set; }
        public string DgProductDesc { get; set; }
        public string EngPartCode { get; set; }
        public string EngPartDesc { get; set; }
        public string JPriority { get; set; }
        public string AltDts { get; set; }
        public string Stage1Status { get; set; }
        public string EngStk { get; set; }

    }
}
