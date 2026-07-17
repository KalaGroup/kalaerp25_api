using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO.Bending
{
    public class BendingCpyKitDto
    {
        public string? KitDesc { get; set; }
        public string? KitCode { get; set; }
        public string? PfbCode { get; set; }
        public string? EDt { get; set; }       // was DateTime? — must be string?
        public decimal? Rate { get; set; }
        public decimal? Strokes { get; set; }
        public string? CatID { get; set; }
        public decimal? Bal { get; set; }
        public decimal? PRate { get; set; }
        public decimal? SRate { get; set; }
        public decimal? Pwt { get; set; }
        public decimal? Psqft { get; set; }
    }
}
