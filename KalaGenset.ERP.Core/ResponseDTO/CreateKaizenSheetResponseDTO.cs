using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class CreateKaizenSheetResponseDTO
    {
        public int Id { get; set; }
        public string KaizenSheetNo { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
