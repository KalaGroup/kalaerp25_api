using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class RaiseReqDtsSub
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartDesc { get; set; } = string.Empty;
        public double Rate { get; set; }
        public double KVA { get; set; }
        public double Strokes { get; set; }
        public string CompCode { get; set; } = string.Empty;
        public string CatID { get; set; } = string.Empty;
    }
}
