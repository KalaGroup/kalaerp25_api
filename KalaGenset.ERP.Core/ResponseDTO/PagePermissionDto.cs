using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KalaGenset.ERP.Core.DTO
{
    public class PagePermissionDto
    {
        public int PageId { get; set; }
        public string PageName { get; set; }
        public bool PermissionStatus { get; set; }
    }
}
