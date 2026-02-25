using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.ResponseDTO
{
    public class KaizenSheetListResponse
    {
        public int Id { get; set; }
        public string KaizenSheetNo { get; set; } = null!;
        public string? DivisionName { get; set; }
        public string? DepartmentName { get; set; }
        public string? WorkstationName { get; set; }
        public string? KaizenTheme { get; set; }
        public string? KaizenInitiationDate { get; set; }
        public string? CompletionDate { get; set; }
        public string? Result { get; set; }
        public string? Improvement { get; set; }
        public string? DataSubmittedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsAuth { get; set; }
    }
}
