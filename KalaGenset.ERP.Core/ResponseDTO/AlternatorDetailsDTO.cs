using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class AlternatorDetailsDTO
    {
        public string QRSrNo { get; set; } = null!;

        public string? Trstatus { get; set; }

        public string? AltDesc { get; set; }

        public string? AltPart { get; set; }

        public string? Stock { get; set; }
    }
}
