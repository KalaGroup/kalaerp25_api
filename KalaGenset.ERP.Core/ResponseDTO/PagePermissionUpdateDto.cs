using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class PagePermissionUpdateDto
    {
        public int PCID { get; set; }
        public int RoleId { get; set; }
        public int PageId { get; set; }
        public bool PermissionStatus { get; set; }
    }
}
