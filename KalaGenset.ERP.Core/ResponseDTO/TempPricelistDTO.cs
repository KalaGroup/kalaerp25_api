using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class TempPricelistDTO
    {
        public string ProfitCenterCode { get; set; } = null!;
        public string PartCode { get; set; } = null!;
        public double PurRate { get; set; }
        public double Rate { get; set; }
        public double Pwt { get; set; }
        public double PsqFt { get; set; }
    }
}
