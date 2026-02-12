using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class PendingAuthQualityListDto
    {
        public int StageWiseQcid { get; set; }
        public string Pccode { get; set; }
        public string PCName { get; set; }
        public string StageName { get; set; }
        public decimal FromKva { get; set; }
        public decimal ToKva { get; set; }
    }
}
